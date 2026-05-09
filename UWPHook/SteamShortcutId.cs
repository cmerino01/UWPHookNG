using System.Text;
using Force.Crc32;

namespace UWPHook;

/// <summary>
/// Computes the local Steam shortcut id used by Steam to link grid art and
/// "now playing" tracking to a non-Steam game.
/// </summary>
internal static class SteamShortcutId
{
    /// <summary>
    /// Generates the CRC32-based id Steam expects for a non-Steam shortcut.
    /// See https://blog.yo1.dog/calculate-id-for-non-steam-games-js/ for the algorithm.
    /// </summary>
    /// <param name="appName">Display name of the shortcut.</param>
    /// <param name="appTarget">Executable target path of the shortcut.</param>
    /// <returns>The 64-bit Steam shortcut id (high bit set).</returns>
    public static ulong Generate(string appName, string appTarget)
    {
        byte[] nameTargetBytes = Encoding.UTF8.GetBytes(appTarget + appName);
        ulong crc = Crc32Algorithm.Compute(nameTargetBytes);
        return crc | 0x80000000UL;
    }
}
