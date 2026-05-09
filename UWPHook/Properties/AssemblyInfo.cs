using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;

// UWPHook is Windows-only by design (UWP discovery, Steam shortcuts, System.Drawing GDI+, WMI).
// Declaring it here removes a flood of CA1416 warnings without changing behavior.
[assembly: SupportedOSPlatform("windows6.1")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.
[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,           // theme-specific resource dictionaries
    ResourceDictionaryLocation.SourceAssembly  // generic resource dictionary
)]
