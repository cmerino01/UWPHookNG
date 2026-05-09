using System.IO;
using UWPHook;
using VDFParser.Models;

namespace UWPHook.Tests;

public class SteamManagerTests
{
    [Fact]
    public void WriteShortcuts_RoundTrip_PreservesEntries()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "UWPHookTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            // SteamManager.ReadShortcuts expects <userPath>/config/shortcuts.vdf, so mirror that layout.
            var configDir = Path.Combine(tmpDir, "config");
            Directory.CreateDirectory(configDir);
            var vdfPath = Path.Combine(configDir, "shortcuts.vdf");

            var entries = new[]
            {
                new VDFEntry
                {
                    appid = 12345,
                    AppName = "Test Game One",
                    Exe = "\"C:\\path with space\\UWPHook.exe\"",
                    StartDir = "\"C:\\path with space\"",
                    LaunchOptions = "Some.Aumid!Game \"game.exe\"",
                    Icon = "C:\\icon.png",
                    Index = 0,
                    IsHidden = 0,
                    OpenVR = 0,
                    AllowDesktopConfig = 1,
                    AllowOverlay = 1,
                    ShortcutPath = "",
                    Tags = new[] { "UWP", "GamePass" },
                    Devkit = 0,
                    DevkitGameID = "",
                    LastPlayTime = 1700000000,
                },
                new VDFEntry
                {
                    appid = -42,
                    AppName = "Another Title",
                    Exe = "\"C:\\foo.exe\"",
                    StartDir = "\"C:\\\"",
                    LaunchOptions = "Pkg!App \"a.exe\"",
                    Icon = "",
                    Index = 1,
                    IsHidden = 0,
                    OpenVR = 0,
                    AllowDesktopConfig = 1,
                    AllowOverlay = 1,
                    ShortcutPath = "",
                    Tags = System.Array.Empty<string>(),
                    Devkit = 0,
                    DevkitGameID = "",
                    LastPlayTime = 0,
                },
            };

            SteamManager.WriteShortcuts(entries, vdfPath);

            Assert.True(File.Exists(vdfPath), "shortcuts.vdf should exist after write");

            var roundTripped = SteamManager.ReadShortcuts(tmpDir);

            Assert.Equal(entries.Length, roundTripped.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                Assert.Equal(entries[i].appid, roundTripped[i].appid);
                Assert.Equal(entries[i].AppName, roundTripped[i].AppName);
                Assert.Equal(entries[i].Exe, roundTripped[i].Exe);
                Assert.Equal(entries[i].StartDir, roundTripped[i].StartDir);
                Assert.Equal(entries[i].LaunchOptions, roundTripped[i].LaunchOptions);
                Assert.Equal(entries[i].Icon, roundTripped[i].Icon);
                Assert.Equal(entries[i].LastPlayTime, roundTripped[i].LastPlayTime);
            }
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void WriteShortcuts_OverExistingFile_ProducesBackup()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "UWPHookTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var configDir = Path.Combine(tmpDir, "config");
            Directory.CreateDirectory(configDir);
            var vdfPath = Path.Combine(configDir, "shortcuts.vdf");

            // Write an initial file so the backup path can fire.
            var first = new[] { MakeEntry("Initial Game", appid: 1, index: 0) };
            SteamManager.WriteShortcuts(first, vdfPath);
            var firstBytes = File.ReadAllBytes(vdfPath);

            // Capture which backups exist before the next write so we can identify the new one.
            var backupDir = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Briano", "UWPHook", "backups");
            var existingBackups = Directory.Exists(backupDir)
                ? new HashSet<string>(Directory.EnumerateFiles(backupDir, "*_shortcuts.vdf"))
                : new HashSet<string>();

            // Second write triggers backup of the first.
            var second = new[] { MakeEntry("Replacement Game", appid: 2, index: 0) };
            SteamManager.WriteShortcuts(second, vdfPath);

            Assert.True(Directory.Exists(backupDir), "Backup directory should exist after a second write.");

            var newBackup = Directory.EnumerateFiles(backupDir, "*_shortcuts.vdf")
                .FirstOrDefault(p => !existingBackups.Contains(p));

            Assert.NotNull(newBackup);
            // Backup should match the bytes of the first write, not the second.
            Assert.Equal(firstBytes, File.ReadAllBytes(newBackup!));
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public void ReadShortcuts_MissingFile_ReturnsEmptyArray()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "UWPHookTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            // No config/ directory at all.
            var result = SteamManager.ReadShortcuts(tmpDir);
            Assert.Empty(result);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    private static VDFEntry MakeEntry(string name, int appid, int index) => new()
    {
        appid = appid,
        AppName = name,
        Exe = "\"C:\\app.exe\"",
        StartDir = "\"C:\\\"",
        LaunchOptions = "",
        Icon = "",
        Index = index,
        IsHidden = 0,
        OpenVR = 0,
        AllowDesktopConfig = 1,
        AllowOverlay = 1,
        ShortcutPath = "",
        Tags = System.Array.Empty<string>(),
        Devkit = 0,
        DevkitGameID = "",
        LastPlayTime = 0,
    };
}
