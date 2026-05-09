using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Serilog;
using VDFParser;
using VDFParser.Models;

namespace UWPHook;

public static class SteamManager
{
    /// <summary>
    /// Maximum number of <c>shortcuts.vdf</c> backups retained per Steam user.
    /// Older backups are pruned on each write.
    /// </summary>
    private const int MaxBackupsPerUser = 20;

    /// <summary>
    /// Returns Steam's current installed path.
    /// </summary>
    public static string? GetSteamFolder()
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
    /// Writes the supplied list of shortcuts to <paramref name="vdfPath"/>.
    /// Before overwriting, the existing file (if any) is copied to
    /// <c>%APPDATA%\Briano\UWPHook\backups\</c> so users can recover from a bad
    /// export. The new file is written via a temp file + atomic move so a crash
    /// mid-write cannot leave a half-written <c>shortcuts.vdf</c>.
    /// </summary>
    public static void WriteShortcuts(VDFEntry[] vdf, string vdfPath)
    {
        BackupExistingShortcuts(vdfPath);

        var bytes = VDFToBytes(vdf);
        var tempPath = vdfPath + ".uwphook.tmp";

        File.WriteAllBytes(tempPath, bytes);
        // Atomic on NTFS; either the new file is in place or it isn't.
        File.Move(tempPath, vdfPath, overwrite: true);
    }

    /// <summary>
    /// Copies the existing shortcuts file (if present) into the backup directory.
    /// Backup filenames embed the Steam user id and a UTC timestamp so multiple
    /// backups don't collide. Old backups are pruned to <see cref="MaxBackupsPerUser"/>.
    /// </summary>
    private static void BackupExistingShortcuts(string vdfPath)
    {
        try
        {
            if (!File.Exists(vdfPath))
            {
                return;
            }

            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Briano", "UWPHook", "backups");

            Directory.CreateDirectory(backupDir);

            // vdfPath is .../userdata/<steamId3>/config/shortcuts.vdf — pull the user id out.
            var userId = "unknown";
            try
            {
                var configDir = Path.GetDirectoryName(vdfPath);
                if (!string.IsNullOrEmpty(configDir))
                {
                    var parent = Directory.GetParent(configDir);
                    if (parent is not null)
                    {
                        userId = parent.Name;
                    }
                }
            }
            catch
            {
                // Best-effort; the timestamp alone is enough to disambiguate.
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupDir, $"{userId}_{timestamp}_shortcuts.vdf");

            File.Copy(vdfPath, backupPath, overwrite: true);
            Log.Information("Backed up existing shortcuts.vdf to {BackupPath}", backupPath);

            PruneOldBackups(backupDir, userId);
        }
        catch (Exception e)
        {
            // Don't let a backup failure prevent the actual write; just log it.
            Log.Warning(e, "Failed to back up existing shortcuts.vdf at {VdfPath}", vdfPath);
        }
    }

    private static void PruneOldBackups(string backupDir, string userId)
    {
        try
        {
            var prefix = userId + "_";
            var stale = new DirectoryInfo(backupDir)
                .EnumerateFiles($"{prefix}*_shortcuts.vdf")
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(MaxBackupsPerUser)
                .ToList();

            foreach (var file in stale)
            {
                try { file.Delete(); }
                catch { /* ignore individual delete failures */ }
            }
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Failed to prune old shortcuts.vdf backups");
        }
    }

    /// <summary>
    /// Converts a <see cref="VDFEntry"/> array to a byte array.
    /// </summary>
    public static byte[] VDFToBytes(VDFEntry[] vdfArray) => VDFSerializer.Serialize(vdfArray);
}
