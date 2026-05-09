using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using Serilog;
using UWPHook.Properties;

namespace UWPHook;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Catch-all handlers so any unhandled exception ends up in the log
        // (file + Visual Studio Output) instead of silently terminating the process.
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled AppDomain exception (terminating={IsTerminating})", args.IsTerminating);

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI dispatcher exception");
            // Don't mark as handled; let the existing message-box flow run.
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        // Apply the persisted theme before any window is shown so the first paint matches.
        var selection = ThemeManager.ParseSelection(Settings.Default.Theme);
        ThemeManager.Apply(selection);

        // Refresh on OS theme changes (only matters in Auto mode; cheap to always wire).
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        base.OnStartup(e);
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            Application.Current?.Dispatcher.BeginInvoke((Action)ThemeManager.ReevaluateFromSystem);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        Log.Information("UWPHook exiting (code {ExitCode})", e.ApplicationExitCode);
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
