using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(AutoCAD2024McpAddin.PluginEntry))]
[assembly: CommandClass(typeof(AutoCAD2024McpAddin.PluginEntry))]

namespace AutoCAD2024McpAddin;

public sealed class PluginEntry : IExtensionApplication
{
    public void Initialize()
    {
        try
        {
            // 1. Initialize Ribbon Tab and Buttons
            RibbonController.Initialize();

            // 2. Start MCP server once on load
            ServerManager.Start();

            WriteCadMessage("AutoCAD 2024 MCP Bridge initialized. Ribbon tab 'AI CAD MCP' ready.");
        }
        catch (System.Exception ex)
        {
            WriteCadMessage($"AutoCAD 2024 MCP Bridge initialization error: {ex.Message}");
        }
    }

    public void Terminate()
    {
        try
        {
            ServerManager.Stop();
        }
        catch { }
    }

    [CommandMethod("C3DMCPSTART")]
    public void StartServerCommand()
    {
        try
        {
            if (ServerManager.Start())
            {
                WriteCadMessage($"MCP Server started on port {ServerManager.DefaultTcpPort}. AI Assistant can connect.");
            }
        }
        catch (System.Exception ex)
        {
            WriteCadMessage($"Failed to start MCP server: {ex.Message}");
        }
    }

    [CommandMethod("C3DMCPREFRESH")]
    public void RefreshServerCommand()
    {
        try
        {
            ServerManager.Refresh();
            WriteCadMessage("MCP Server and drawing connection refreshed.");
        }
        catch (System.Exception ex)
        {
            WriteCadMessage($"Failed to refresh MCP server: {ex.Message}");
        }
    }

    [CommandMethod("C3DMCPSTOP")]
    public void StopServerCommand()
    {
        try
        {
            ServerManager.Stop();
            WriteCadMessage("MCP Server stopped.");
        }
        catch (System.Exception ex)
        {
            WriteCadMessage($"Failed to stop MCP server: {ex.Message}");
        }
    }

    [CommandMethod("C3DMCPSTATUS")]
    public void StatusCommand()
    {
        bool running = ServerManager.IsRunning;
        string uptime = ServerManager.StartedAt.HasValue
            ? $"{(DateTime.Now - ServerManager.StartedAt.Value).TotalMinutes:F1} min"
            : "Offline";

        string docName = App.DocumentManager.MdiActiveDocument != null
            ? Path.GetFileName(App.DocumentManager.MdiActiveDocument.Name)
            : "<None>";

        string msg = $"--- AutoCAD 2024 MCP Server Status ---\n" +
                     $"Listener: {(running ? "ONLINE (Active)" : "OFFLINE")}\n" +
                     $"TCP Port: {ServerManager.DefaultTcpPort}\n" +
                     $"Uptime: {uptime}\n" +
                     $"Requests Processed: {ServerManager.HandledRequestCount}\n" +
                     $"Last Operation: {ServerManager.LastOperation ?? "<none>"}\n" +
                     $"Active Drawing: {docName}";

        WriteCadMessage(msg);
        MessageBox.Show(msg, "Civil 3D / AutoCAD MCP Server Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    [CommandMethod("C3DMCPPALETTE")]
    public void TogglePaletteCommand()
    {
        PaletteController.TogglePalette();
    }

    [CommandMethod("C3DTBDUMP")]
    [CommandMethod("TBDUMP")]
    public void DumpTitleBlockCommand()
    {
        try
        {
            string outPath = TitleBlockManager.DumpActiveLayoutTitleBlock();
            WriteCadMessage($"Title Block dump saved: {outPath}");
        }
        catch (System.Exception ex)
        {
            WriteCadMessage($"Error dumping title block: {ex.Message}");
        }
    }

    [CommandMethod("C3DTBREV")]
    [CommandMethod("TBREV")]
    public void UpdateTitleBlockRevCommand()
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc == null) return;

        var pso = new PromptStringOptions("\nEnter new revision letter [e.g. B]: ")
        {
            AllowSpaces = false,
            DefaultValue = "B"
        };
        var res = doc.Editor.GetString(pso);
        if (res.Status != PromptStatus.OK) return;

        string rev = res.StringResult.Trim().ToUpperInvariant();
        try
        {
            bool ok = TitleBlockManager.UpdateRevision(rev);
            if (ok)
            {
                WriteCadMessage($"Revision successfully updated to '{rev}'.");
            }
            else
            {
                WriteCadMessage("Warning: No matching title block revision attributes found in active space.");
            }
        }
        catch (System.Exception ex)
        {
            WriteCadMessage($"Error updating title block: {ex.Message}");
        }
    }

    [CommandMethod("C3DTBBATCH")]
    public void RunBatchCommand()
    {
        string batchFolder = Path.Combine(ServerManager.SetupDirectory, "tb_batch");
        if (Directory.Exists(batchFolder))
        {
            Process.Start("explorer.exe", batchFolder);
            WriteCadMessage($"Opened Title Block batch folder: {batchFolder}\nRun Run-TitleBlockBatch.ps1 with PowerShell to process drawing batches.");
        }
        else
        {
            WriteCadMessage($"Batch directory not found at {batchFolder}");
        }
    }

    private static void WriteCadMessage(string msg)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage($"\n{msg}");
    }
}
