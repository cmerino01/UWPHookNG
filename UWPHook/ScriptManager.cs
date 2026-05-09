using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace UWPHook;

/// <summary>
/// Functions related to Windows PowerShell
/// </summary>
static class ScriptManager
{
    public static string RunScript(string scriptText)
    {
        using var runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();

        var pipeline = runspace.CreatePipeline();
        pipeline.Commands.AddScript(scriptText);

        // Format the output objects as strings for easy consumption.
        pipeline.Commands.Add("Out-String");

        var results = pipeline.Invoke();

        var sb = new StringBuilder();
        foreach (var obj in results)
        {
            sb.AppendLine(obj.ToString());
        }

        return sb.ToString();
    }
}
