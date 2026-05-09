using UWPHook;
using UWPHook.Properties;

namespace UWPHook.Tests;

/// <summary>
/// Tests for ProtectedApiKey share the application's Settings singleton, so they
/// run sequentially within this collection to avoid interleaved Get/Set steps.
/// </summary>
[Collection("ProtectedApiKey")]
public class ProtectedApiKeyTests : IDisposable
{
    private readonly string? _previousValue;

    public ProtectedApiKeyTests()
    {
        // Snapshot the current setting so the test run leaves user.config alone.
        _previousValue = Settings.Default.SteamGridDbApiKey;
        Settings.Default.SteamGridDbApiKey = string.Empty;
    }

    public void Dispose()
    {
        Settings.Default.SteamGridDbApiKey = _previousValue ?? string.Empty;
    }

    [Fact]
    public void RoundTrip_PreservesPlaintext()
    {
        const string secret = "my-super-secret-api-key-12345";

        ProtectedApiKey.SetSteamGridDbApiKey(secret);

        // The on-disk form must not be the plaintext.
        Assert.NotEqual(secret, Settings.Default.SteamGridDbApiKey);
        Assert.StartsWith("dpapi:", Settings.Default.SteamGridDbApiKey);

        Assert.Equal(secret, ProtectedApiKey.GetSteamGridDbApiKey());
    }

    [Fact]
    public void Empty_StoresEmptyString_NotProtectedBlob()
    {
        ProtectedApiKey.SetSteamGridDbApiKey("");
        Assert.Equal(string.Empty, Settings.Default.SteamGridDbApiKey);

        ProtectedApiKey.SetSteamGridDbApiKey("   ");
        Assert.Equal(string.Empty, Settings.Default.SteamGridDbApiKey);

        ProtectedApiKey.SetSteamGridDbApiKey(null);
        Assert.Equal(string.Empty, Settings.Default.SteamGridDbApiKey);
    }

    [Fact]
    public void LegacyPlaintextValue_IsReturnedAsIs()
    {
        // Simulate an upgrade from a pre-DPAPI install where the value was stored raw.
        const string legacy = "legacy-plaintext-key";
        Settings.Default.SteamGridDbApiKey = legacy;

        Assert.Equal(legacy, ProtectedApiKey.GetSteamGridDbApiKey());
    }

    [Fact]
    public void HasKey_TracksGetterTruthiness()
    {
        ProtectedApiKey.SetSteamGridDbApiKey(string.Empty);
        Assert.False(ProtectedApiKey.HasSteamGridDbApiKey());

        ProtectedApiKey.SetSteamGridDbApiKey("non-empty");
        Assert.True(ProtectedApiKey.HasSteamGridDbApiKey());
    }
}
