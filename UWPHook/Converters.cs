using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UWPHook;

/// <summary>
/// Converts an <see cref="AppEntry"/> to an <see cref="ImageSource"/> suitable for a tile.
/// Falls back through the available image sources rather than failing:
///   1. <see cref="AppEntry.Icon"/> (already-resolved icon path on disk)
///   2. The widest square logo found under <see cref="AppEntry.IconPath"/>
///   3. <c>null</c> — the tile renders a letter-tile fallback in XAML.
/// Decoded bitmaps are cached by path + last-write-time to avoid re-decoding the
/// same multi-hundred-KB UWP logo every time the tile rebinds (selection click,
/// search filter, view toggle, etc.).
/// </summary>
public sealed class AppEntryToBoxArtConverter : IValueConverter
{
    /// <summary>Tiles render at ~180px wide; decoding to ~360px keeps them crisp on hi-DPI.</summary>
    private const int TargetDecodePixelWidth = 360;

    private static readonly ConcurrentDictionary<string, BitmapImage> s_cache = new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not AppEntry app)
        {
            return null;
        }

        var path = !string.IsNullOrEmpty(app.Icon) && File.Exists(app.Icon)
            ? app.Icon
            : SafeWidestIcon(app);

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return null;
        }

        // Key includes the file's last-write-time so a regenerated icon invalidates the cache entry.
        string key;
        try
        {
            key = path + "|" + File.GetLastWriteTimeUtc(path).Ticks.ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            key = path;
        }

        if (s_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            // OnLoad detaches the file handle and decodes synchronously into memory.
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            // Decoding small dramatically reduces both memory and CPU when binding many tiles.
            bmp.DecodePixelWidth = TargetDecodePixelWidth;
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            s_cache[key] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static string SafeWidestIcon(AppEntry app)
    {
        try { return app.widestSquareIcon(); }
        catch { return string.Empty; }
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}

/// <summary>
/// Returns the first character of a string (or "?") as an uppercase letter for the
/// generated letter-tile fallback when no icon is available.
/// </summary>
public sealed class FirstLetterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && s.Length > 0)
        {
            return char.ToUpperInvariant(s[0]).ToString();
        }
        return "?";
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}

/// <summary>
/// Maps a string to a deterministic color, used to tint letter-tile fallbacks.
/// </summary>
public sealed class StringToColorBrushConverter : IValueConverter
{
    private static readonly Color[] s_palette = new[]
    {
        Color.FromRgb(0x10, 0x7C, 0x10), // Xbox green
        Color.FromRgb(0x21, 0x5A, 0x9C), // steam blue
        Color.FromRgb(0xC2, 0x39, 0x4F), // ruby
        Color.FromRgb(0xC2, 0x77, 0x1B), // amber
        Color.FromRgb(0x7A, 0x2F, 0x9C), // violet
        Color.FromRgb(0x16, 0x83, 0x8B), // teal
        Color.FromRgb(0xB6, 0x3A, 0x10), // ember
        Color.FromRgb(0x2C, 0x6E, 0x49), // moss
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value as string ?? string.Empty;
        var hash = 0;
        for (int i = 0; i < key.Length; i++)
        {
            hash = unchecked(hash * 31 + key[i]);
        }
        var index = (int)((uint)hash % s_palette.Length);
        var brush = new SolidColorBrush(s_palette[index]);
        brush.Freeze();
        return brush;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}

/// <summary>
/// True when a string is null, empty, or whitespace; useful for
/// <c>Visibility</c> bindings on empty-state placeholders.
/// </summary>
public sealed class NullOrEmptyToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visibleWhenEmpty = !(parameter is string p && p == "inverse");
        var isEmpty = value is null
            || (value is string s && string.IsNullOrWhiteSpace(s));
        return (isEmpty == visibleWhenEmpty)
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}

/// <summary>
/// Returns Visible when the bound value (count) is greater than zero.
/// </summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int i => i,
            long l => (int)l,
            _ => 0,
        };
        return count > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
