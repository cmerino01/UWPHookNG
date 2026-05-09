using System.IO;
using Microsoft.Win32;
using VDFParser;
using VDFParser.Models;

namespace UWPHook;

public static class SteamManager
{
    /// <summary>
    /// Returns Steam's current installed path.
    /// </summary>
    public static string GetSteamFolder()
    {
        const string registryPath = @"SOFTWARE\Valve\Steam";

        // Check 64-bit registry view
        using (var localKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
        using (var key64 = localKey64.OpenSubKey(registryPath))
        {
            if (key64?.GetValue("InstallPath") is string path64 && !string.IsNullOrEmpty(path64))
            {
                return path64;
            }
        }

        // Check 32-bit registry view
        using (var localKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        using (var key32 = localKey32.OpenSubKey(registryPath))
        {
            if (key32?.GetValue("InstallPath") is string path32 && !string.IsNullOrEmpty(path32))
            {
                return path32;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all user directories under <c>userdata</c>.
    /// </summary>
    /// <param name="steamInstallPath">Steam's current installed path</param>
    public static string[] GetUsers(string steamInstallPath) =>
        Directory.GetDirectories(Path.Combine(steamInstallPath, "userdata"));

    /// <summary>
    /// Reads the shortcuts present in the <c>shortcuts.vdf</c> file for a given user path.
    /// </summary>
    /// <param name="userPath">The user data path to search for shortcuts</param>
    /// <returns>An array of <see cref="VDFEntry"/></returns>
    public static VDFEntry[] ReadShortcuts(string userPath)
    {
        var shortcutFile = Path.Combine(userPath, "config", "shortcuts.vdf");

        // Some users don't have the config directory or the shortcut file; return an empty array.
        if (!Directory.Exists(Path.Combine(userPath, "config")) || !File.Exists(shortcutFile))
        {
            return System.Array.Empty<VDFEntry>();
        }

        return VDFParser.VDFParser.Parse(shortcutFile);
    }

    /// <summary>
    /// Writes the supplied list of shortcuts to the specified path.
    /// </summary>
    public static void WriteShortcuts(VDFEntry[] vdf, string vdfPath)
    {
        File.WriteAllBytes(vdfPath, VDFToBytes(vdf));
    }

    /// <summary>
    /// Converts a <see cref="VDFEntry"/> array to a byte array.
    /// </summary>
    public static byte[] VDFToBytes(VDFEntry[] vdfArray) => VDFSerializer.Serialize(vdfArray);
}
