using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;

// UWPHook is Windows-only by design (UWP discovery, Steam shortcuts, System.Drawing GDI+, WMI).
// We require Windows 10 (1809+) for the WinRT package enumeration APIs.
[assembly: SupportedOSPlatform("windows10.0.17763.0")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.
[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,           // theme-specific resource dictionaries
    ResourceDictionaryLocation.SourceAssembly  // generic resource dictionary
)]
