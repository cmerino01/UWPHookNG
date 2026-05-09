using UWPHook;

namespace UWPHook.Tests;

public class SteamShortcutIdTests
{
    [Fact]
    public void Generate_IsDeterministic_ForSameInputs()
    {
        var a = SteamShortcutId.Generate("Game A", @"C:\Path\UWPHook.exe");
        var b = SteamShortcutId.Generate("Game A", @"C:\Path\UWPHook.exe");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Generate_HighBitIsSet()
    {
        // Steam shortcut ids always have the high (32-bit) bit set so they don't collide with real appids.
        var id = SteamShortcutId.Generate("Anything", @"C:\anywhere.exe");
        Assert.NotEqual(0UL, id & 0x80000000UL);
    }

    [Fact]
    public void Generate_DifferentNames_ProduceDifferentIds()
    {
        var a = SteamShortcutId.Generate("Game A", @"C:\UWPHook.exe");
        var b = SteamShortcutId.Generate("Game B", @"C:\UWPHook.exe");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Generate_DifferentTargets_ProduceDifferentIds()
    {
        var a = SteamShortcutId.Generate("Same Game", @"C:\path1\UWPHook.exe");
        var b = SteamShortcutId.Generate("Same Game", @"C:\path2\UWPHook.exe");
        Assert.NotEqual(a, b);
    }
}
