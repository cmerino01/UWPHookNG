using System;
using System.Linq;
using System.Windows;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Serilog;

namespace UWPHook;

/// <summary>
/// Selectable application theme. Stored verbatim in user settings.
/// </summary>
internal enum AppTheme
{
    Auto,
    Dark,
    Light,
}

/// <summary>
/// Swaps the active palette dictionary and the Material Design base theme at runtime.
/// All consumer brushes use DynamicResource on the palette color keys so the swap is live.
/// </summary>
internal static class ThemeManager
{
    private const string DarkPaletteUri = "pack://application:,,,/UWPHook;component/Themes/Palette.Dark.xaml";
    private const string LightPaletteUri = "pack://application:,,,/UWPHook;component/Themes/Palette.Light.xaml";

    private static AppTheme s_currentSelection = AppTheme.Auto;

    /// <summary>Theme the user picked (Auto/Dark/Light).</summary>
    public static AppTheme CurrentSelection => s_currentSelection;

    /// <summary>Resolved theme actually in effect (Auto resolves to the OS preference).</summary>
    public static AppTheme ResolvedTheme { get; private set; } = AppTheme.Dark;

    /// <summary>Apply the supplied selection and persist it via the resolver below.</summary>
    public static void Apply(AppTheme selection)
    {
        s_currentSelection = selection;
        ResolvedTheme = Resolve(selection);
        SwapPalette(ResolvedTheme);
        SwapMaterialBaseTheme(ResolvedTheme);
    }

    /// <summary>
    /// Re-resolve when the OS theme changes; only meaningful in <see cref="AppTheme.Auto"/>.
    /// </summary>
    public static void ReevaluateFromSystem()
    {
        if (s_currentSelection == AppTheme.Auto)
        {
            Apply(AppTheme.Auto);
        }
    }

    private static AppTheme Resolve(AppTheme selection) => selection switch
    {
        AppTheme.Dark => AppTheme.Dark,
        AppTheme.Light => AppTheme.Light,
        _ => DetectSystemTheme(),
    };

    /// <summary>
    /// Reads the Windows "AppsUseLightTheme" registry value. 0 = dark, 1 = light.
    /// Falls back to Dark on any error so behavior is predictable.
    /// </summary>
    private static AppTheme DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int v)
            {
                return v == 0 ? AppTheme.Dark : AppTheme.Light;
            }
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Could not read system theme; defaulting to Dark");
        }
        return AppTheme.Dark;
    }

    private static void SwapPalette(AppTheme theme)
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;

        var newSource = new Uri(theme == AppTheme.Light ? LightPaletteUri : DarkPaletteUri, UriKind.Absolute);

        // Find the existing palette dictionary by matching the source path against either palette URI.
        ResourceDictionary? existing = null;
        for (int i = 0; i < resources.MergedDictionaries.Count; i++)
        {
            var src = resources.MergedDictionaries[i].Source?.ToString();
            if (src is null) continue;
            if (src.EndsWith("Palette.Dark.xaml", StringComparison.OrdinalIgnoreCase)
                || src.EndsWith("Palette.Light.xaml", StringComparison.OrdinalIgnoreCase))
            {
                existing = resources.MergedDictionaries[i];
                break;
            }
        }

        var replacement = new ResourceDictionary { Source = newSource };

        if (existing is null)
        {
            resources.MergedDictionaries.Add(replacement);
            return;
        }

        var index = resources.MergedDictionaries.IndexOf(existing);
        resources.MergedDictionaries[index] = replacement;
    }

    private static void SwapMaterialBaseTheme(AppTheme theme)
    {
        try
        {
            var helper = new PaletteHelper();
            var t = helper.GetTheme();
            t.SetBaseTheme(theme == AppTheme.Light ? BaseTheme.Light : BaseTheme.Dark);
            helper.SetTheme(t);
        }
        catch (Exception e)
        {
            // Material Design's theme helper occasionally throws on first call before the visual
            // tree is up; non-fatal — our own brushes have already been swapped.
            Log.Verbose(e, "Could not update Material Design base theme");
        }
    }

    /// <summary>Parses a setting string into an enum value.</summary>
    public static AppTheme ParseSelection(string? raw) =>
        Enum.TryParse<AppTheme>(raw, ignoreCase: true, out var t) ? t : AppTheme.Auto;
}
