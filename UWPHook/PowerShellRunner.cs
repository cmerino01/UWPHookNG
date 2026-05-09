using System;
using System.Diagnostics;
using Serilog;

namespace UWPHook;

/// <summary>
/// Lightweight wrapper that runs a PowerShell command in a child <c>powershell.exe</c>
/// process. Used for the small handful of cmdlets we still rely on (language / display
/// overrides) so we don't have to host the PowerShell SDK in-process.
/// </summary>
internal static class PowerShellRunner
{
    public static void Run(string command)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            proc?.WaitForExit();
        }
        catch (Exception e)
        {
            Log.Warning(e, "PowerShell command failed: {Command}", command);
        }
    }
}
