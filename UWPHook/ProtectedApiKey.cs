using System;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using UWPHook.Properties;

namespace UWPHook;

/// <summary>
/// Wraps the SteamGridDB API key in DPAPI (CurrentUser scope) so the value stored
/// in user.config can't be read by other Windows users or by anyone who copies the
/// file off the machine.
/// </summary>
/// <remarks>
/// Storage format: a sentinel prefix followed by Base64(<see cref="ProtectedData.Protect"/>).
/// The sentinel lets us round-trip migration cleanly: a legacy plaintext value (no
/// sentinel) is read as-is and re-encrypted on the next save. A protected value that
/// fails to decrypt (file moved between machines / users) is treated as missing.
/// </remarks>
internal static class ProtectedApiKey
{
    private const string ProtectedPrefix = "dpapi:v1:";
    private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes("UWPHook.SteamGridDbApiKey.v1");

    /// <summary>
    /// Returns the plaintext API key, or <see cref="string.Empty"/> if not set / unrecoverable.
    /// </summary>
    public static string GetSteamGridDbApiKey()
    {
        var raw = Settings.Default.SteamGridDbApiKey ?? string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        if (!raw.StartsWith(ProtectedPrefix, StringComparison.Ordinal))
        {
            // Legacy plaintext value from a pre-DPAPI install. Return as-is; the next save
            // through SetSteamGridDbApiKey will upgrade it to the protected format.
            return raw;
        }

        try
        {
            var blob = Convert.FromBase64String(raw[ProtectedPrefix.Length..]);
            var plain = ProtectedData.Unprotect(blob, s_entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception e)
        {
            // Most common cause: the file came from another Windows user / machine, so the
            // DPAPI master key is not available. We surface this as "no key" and clear the
            // stored value so the user is prompted to re-enter it.
            Log.Warning(e, "Failed to decrypt SteamGridDB API key; clearing stored value");
            Settings.Default.SteamGridDbApiKey = string.Empty;
            try { Settings.Default.Save(); } catch { /* best-effort */ }
            return string.Empty;
        }
    }

    /// <summary>
    /// Encrypts and persists the API key. Passing <see cref="string.Empty"/> or whitespace
    /// clears the stored value.
    /// </summary>
    public static void SetSteamGridDbApiKey(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            Settings.Default.SteamGridDbApiKey = string.Empty;
            return;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var blob = ProtectedData.Protect(bytes, s_entropy, DataProtectionScope.CurrentUser);
            Settings.Default.SteamGridDbApiKey = ProtectedPrefix + Convert.ToBase64String(blob);
        }
        catch (Exception e)
        {
            // DPAPI itself failing is a Windows-level oddity (e.g. user profile not loaded).
            // Fall back to plaintext storage so the feature still works; log loudly so the
            // user / dev sees it.
            Log.Error(e, "DPAPI Protect failed; storing SteamGridDB API key in plaintext as a fallback");
            Settings.Default.SteamGridDbApiKey = plaintext;
        }
    }

    /// <summary>
    /// True if a (recoverable) API key is configured.
    /// </summary>
    public static bool HasSteamGridDbApiKey() => !string.IsNullOrEmpty(GetSteamGridDbApiKey());
}
