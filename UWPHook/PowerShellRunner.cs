using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace UWPHook;

/// <summary>
/// Spawns <c>powershell.exe</c> for the small set of cmdlets we still rely on
/// (UI language override, display resolution override). Each entry point validates
/// its inputs against a strict allow-list so a malicious or corrupted user.config
/// can't smuggle arbitrary script through.
/// </summary>
internal static class PowerShellRunner
{
    private static readonly Regex s_resolutionPattern = new(@"^\s*(?<w>\d{3,5})\s*[xX]\s*(?<h>\d{3,5})\s*$", RegexOptions.Compiled);
    private static readonly Lazy<HashSet<string>> s_knownCultures = new(() =>
        new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.AllCultures).Select(c => c.Name),
            StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Sets the Windows UI language override for the current user. <paramref name="cultureName"/>
    /// is rejected unless it matches a culture name returned by <see cref="CultureInfo.GetCultures"/>.
    /// </summary>
    public static void SetWinUILanguageOverride(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return;
        }

        if (!s_knownCultures.Value.Contains(cultureName))
        {
            Log.Warning("Refusing to apply unknown culture name {Culture}", cultureName);
            return;
        }

        // Culture names from CultureInfo are alphanumeric/-/_ only — safe to interpolate
        // after the allow-list check above, but we still pass through PowerShell's own
        // single-quoted string escaping for defense-in-depth.
        var escaped = cultureName.Replace("'", "''");
        RunInternal($"Set-WinUILanguageOverride '{escaped}'");
    }

    /// <summary>
    /// Sets the display resolution. <paramref name="resolution"/> must be in the form
    /// "WIDTH x HEIGHT" with reasonable digit counts; anything else is rejected.
    /// </summary>
    public static void SetDisplayResolution(string? resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
        {
            return;
        }

        var match = s_resolutionPattern.Match(resolution);
        if (!match.Success)
        {
            Log.Warning("Refusing to apply malformed resolution {Resolution}", resolution);
            return;
        }

        var width = match.Groups["w"].Value;
        var height = match.Groups["h"].Value;
        RunInternal($"Set-DisplayResolution -Width {width} -Height {height} -Force");
    }

    private static void RunInternal(string command)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            proc?.WaitForExit();
        }
        catch (Exception e)
        {
            Log.Warning(e, "PowerShell command failed: {Command}", command);
        }
    }
}
