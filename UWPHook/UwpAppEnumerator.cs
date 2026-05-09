using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Serilog;
using Windows.Management.Deployment;

namespace UWPHook;

/// <summary>
/// Enumerates installed UWP / MSIX packaged apps for the current user using the Windows
/// runtime APIs (the same data sources <c>Get-AppxPackage</c> uses), avoiding any in-process
/// PowerShell hosting.
/// </summary>
internal static class UwpAppEnumerator
{
    private static readonly XNamespace AppxNs = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly XNamespace UapNs = "http://schemas.microsoft.com/appx/manifest/uap/windows10";

    /// <summary>
    /// Returns one entry per launchable application using the format
    /// <c>Name|LogoPath|AUMID|Executable|Kind</c>, where <c>Kind</c> is
    /// "game" if the package is detected as a game (via MicrosoftGame.Config) and "app" otherwise.
    /// </summary>
    public static List<string> GetInstalledApps()
    {
        var results = new List<string>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var manager = new PackageManager();
        IEnumerable<Windows.ApplicationModel.Package> packages;
        try
        {
            packages = manager.FindPackagesForUser(string.Empty);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to enumerate packages for current user");
            throw;
        }

        foreach (var package in packages)
        {
            try
            {
                if (package.IsFramework || package.IsResourcePackage)
                {
                    continue;
                }

                string? installLocation = TryGetInstalledPath(package);
                if (string.IsNullOrEmpty(installLocation))
                {
                    continue;
                }

                var manifestPath = Path.Combine(installLocation, "AppxManifest.xml");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                XDocument manifest;
                try
                {
                    manifest = XDocument.Load(manifestPath);
                }
                catch (Exception e)
                {
                    Log.Verbose(e, "Skipping unreadable manifest at {ManifestPath}", manifestPath);
                    continue;
                }

                var displayName = manifest
                    .Descendants(AppxNs + "Properties")
                    .Elements(AppxNs + "DisplayName")
                    .Select(e => e.Value)
                    .FirstOrDefault() ?? string.Empty;

                if (LooksLikeResourceRef(displayName))
                {
                    // Skip apps whose display name we can't resolve (matches the legacy script).
                    continue;
                }

                var familyName = package.Id.FamilyName;
                var hasGameConfig = File.Exists(Path.Combine(installLocation, "MicrosoftGame.Config"));

                foreach (var application in manifest.Descendants(AppxNs + "Application"))
                {
                    var appId = application.Attribute("Id")?.Value;
                    if (string.IsNullOrEmpty(appId))
                    {
                        continue;
                    }

                    var executable = application.Attribute("Executable")?.Value ?? string.Empty;
                    var executableViaGameConfig = false;

                    // Apps launched via the Microsoft Game launcher have no real executable in
                    // the manifest; fall back to MicrosoftGame.Config (Game Pass titles).
                    if (string.IsNullOrWhiteSpace(executable) ||
                        string.Equals(executable, "GameLaunchHelper.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var fromConfig = TryReadGameConfigExecutable(installLocation);
                        if (!string.IsNullOrWhiteSpace(fromConfig))
                        {
                            executable = fromConfig;
                            executableViaGameConfig = true;
                        }
                        if (string.IsNullOrWhiteSpace(executable))
                        {
                            continue;
                        }
                    }

                    var logoRelative = application
                        .Elements(UapNs + "VisualElements")
                        .Select(e => e.Attribute("Square150x150Logo")?.Value)
                        .FirstOrDefault(v => !string.IsNullOrEmpty(v));
                    var logoPath = !string.IsNullOrEmpty(logoRelative)
                        ? Path.Combine(installLocation, logoRelative!)
                        : string.Empty;

                    // Suppress duplicates that share a leading display name (e.g. Halo MCC ships
                    // two AUMIDs differing only by anti-cheat suffix).
                    if (!seenNames.Add(displayName))
                    {
                        continue;
                    }

                    var aumid = $"{familyName}!{appId}";

                    // Heuristic: an app is a "game" if MicrosoftGame.Config is present (canonical
                    // XGP / MS Store-game signal) or if we had to resolve its executable through
                    // the game launcher fallback above.
                    var isGame = hasGameConfig || executableViaGameConfig;

                    results.Add($"{displayName}|{logoPath}|{aumid}|{executable}|{(isGame ? "game" : "app")}");
                }
            }
            catch (Exception e)
            {
                Log.Verbose(e, "Skipping package {PackageFullName}", SafePackageName(package));
            }
        }

        return results;
    }

    private static string? TryGetInstalledPath(Windows.ApplicationModel.Package package)
    {
        try
        {
            return package.InstalledLocation?.Path;
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Could not access install location for {PackageFullName}", SafePackageName(package));
            return null;
        }
    }

    private static string? TryReadGameConfigExecutable(string installLocation)
    {
        var configPath = Path.Combine(installLocation, "MicrosoftGame.Config");
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Load(configPath);
            var executables = doc.Descendants("Executable")
                .Select(e => e.Attribute("Name")?.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            // Halo MCC and friends list a stub launcher first and the real game second.
            return executables.Count > 1 ? executables[1] : executables.FirstOrDefault();
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Failed to read MicrosoftGame.Config at {ConfigPath}", configPath);
            return null;
        }
    }

    private static bool LooksLikeResourceRef(string displayName) =>
        displayName.Contains("ms-resource", StringComparison.OrdinalIgnoreCase) ||
        displayName.Contains("DisplayName", StringComparison.OrdinalIgnoreCase);

    private static string SafePackageName(Windows.ApplicationModel.Package package)
    {
        try { return package.Id.FullName; }
        catch { return "<unknown>"; }
    }
}
