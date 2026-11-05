using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCAD2024McpAddin;

public static class ServerManager
{
    public const int DefaultTcpPort = 8080;
    public const int DefaultHttpPort = 3000;
    public static readonly string SetupDirectory = @"C:\RevitAddinDev\CIVIL 3D_MCP Setup";

    private static readonly object _sync = new();
    private static TcpListener? _listener;
    private static CancellationTokenSource? _cts;
    private static Process? _nodeProcess;
    private static bool _isRunning;
    private static DateTime? _startedAt;
    private static int _handledRequestCount;
    private static string? _lastOperation;

    public static bool IsRunning => _isRunning;
    public static DateTime? StartedAt => _startedAt;
    public static int HandledRequestCount => _handledRequestCount;
    public static string? LastOperation => _lastOperation;

    public static bool Start()
    {
        lock (_sync)
        {
            if (_isRunning)
            {
                LogInfo($"MCP Server is already running on port {DefaultTcpPort}.");
                return true;
            }

            // Check if port is already bound
            if (IsPortBound(DefaultTcpPort))
            {
                if (CanPingServer(DefaultTcpPort))
                {
                    _isRunning = true;
                    _startedAt = DateTime.Now;
                    LogInfo($"MCP Server listener on port {DefaultTcpPort} is already active and responding.");
                    EnsureNodeMcpServerRunning();
                    return true;
                }
                else
                {
                    LogWarning($"Port {DefaultTcpPort} is in use by another process. Use 'Refresh Server' or check background processes.");
                    return false;
                }
            }

            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, DefaultTcpPort);
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Start(10);
                _isRunning = true;
                _startedAt = DateTime.Now;

                Task.Run(() => AcceptClientsAsync(_listener, _cts.Token));

                // Auto-launch Node MCP server without inheriting parent socket handles
                EnsureNodeMcpServerRunning();

                LogInfo($"AutoCAD 2024 MCP Server started on port {DefaultTcpPort}.");
                return true;
            }
            catch (SocketException sockEx) when (sockEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                _isRunning = false;
                LogWarning($"Port {DefaultTcpPort} is already bound. Click 'Refresh Server' on the ribbon to reset.");
                return false;
            }
            catch (Exception ex)
            {
                _isRunning = false;
                LogError("Failed to start MCP server", ex);
                return false;
            }
        }
    }

    public static void Refresh()
    {
        lock (_sync)
        {
            LogInfo("Refreshing MCP Server and connections...");
            Stop();
            Thread.Sleep(600);
            Start();
            LogInfo("MCP Server refreshed.");
        }
    }

    public static void Stop()
    {
        lock (_sync)
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener = null;
                _cts = null;
                _isRunning = false;

                if (_nodeProcess != null && !_nodeProcess.HasExited)
                {
                    try
                    {
                        _nodeProcess.Kill();
                        _nodeProcess.Dispose();
                    }
                    catch { }
                    _nodeProcess = null;
                }

                LogInfo("AutoCAD 2024 MCP Server stopped.");
            }
            catch (Exception ex)
            {
                LogError("Error while stopping server", ex);
            }
        }
    }

    private static bool IsPortBound(int port)
    {
        try
        {
            var ipProps = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = ipProps.GetActiveTcpListeners();
            foreach (var ep in listeners)
            {
                if (ep.Port == port) return true;
            }
        }
        catch { }
        return false;
    }

    private static bool CanPingServer(int port)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync("127.0.0.1", port);
                if (connectTask.Wait(400) && client.Connected)
                {
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static async Task AcceptClientsAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
            catch when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogError("Error accepting client", ex);
            }
        }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
        {
            while (!ct.IsCancellationRequested && client.Connected)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync();
                }
                catch
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                string response = ProcessJsonRpcRequest(line);
                try
                {
                    await writer.WriteLineAsync(response);
                }
                catch
                {
                    break;
                }
            }
        }
    }

    private static string ProcessJsonRpcRequest(string rawJson)
    {
        JsonNode? idNode = null;
        try
        {
            var root = JsonNode.Parse(rawJson);
            if (root == null) throw new InvalidOperationException("Empty JSON");

            idNode = root["id"];
            string method = root["method"]?.GetValue<string>() ?? "";
            var @params = root["params"] as JsonObject ?? new JsonObject();

            _lastOperation = method;
            Interlocked.Increment(ref _handledRequestCount);

            object? result = DispatchMethod(method, @params);

            var resp = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = idNode?.DeepClone() ?? (JsonNode)"1",
                ["result"] = JsonSerializer.SerializeToNode(result)
            };
            return resp.ToJsonString();
        }
        catch (Exception ex)
        {
            var errResp = new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = idNode?.DeepClone(),
                ["error"] = new JsonObject
                {
                    ["code"] = -32603,
                    ["message"] = ex.Message
                }
            };
            return errResp.ToJsonString();
        }
    }

    private static object? DispatchMethod(string method, JsonObject @params)
    {
        switch (method.ToLowerInvariant())
        {
            case "ping":
            case "civil3d_health":
            case "health":
                return new
                {
                    status = "ok",
                    version = "1.2.1-acad2024",
                    host = "AutoCAD 2024",
                    serverRunning = _isRunning,
                    uptimeSeconds = _startedAt.HasValue ? (DateTime.Now - _startedAt.Value).TotalSeconds : 0
                };

            case "getdrawinginfo":
                return GetDrawingInfo();

            case "getdrawingsettings":
                return GetDrawingSettings();

            case "savedrawing":
                return SaveDrawing(@params["saveAs"]?.GetValue<string>());

            case "dumptitleblock":
                string? outPath = @params["outputPath"]?.GetValue<string>();
                string path = TitleBlockManager.DumpActiveLayoutTitleBlock(outPath);
                return new { success = true, outputPath = path };

            case "updatetitleblockrevision":
                string rev = @params["revision"]?.GetValue<string>() ?? "A";
                string? desc = @params["description"]?.GetValue<string>();
                string? date = @params["date"]?.GetValue<string>();
                bool updated = TitleBlockManager.UpdateRevision(rev, desc, date);
                return new { success = updated, revision = rev };

            case "listcivilobjecttypes":
                return new[]
                {
                    "AcDbBlockReference",
                    "AcDbText",
                    "AcDbMText",
                    "AcDbLine",
                    "AcDbPolyline",
                    "AcDb2dPolyline",
                    "AcDb3dPolyline",
                    "AcDbHatch",
                    "AcDbDimension",
                    "AcDbTable",
                    "TitleBlock",
                    "SheetLayout"
                };

            default:
                throw new NotSupportedException($"Method '{method}' is not supported in AutoCAD 2024 mode.");
        }
    }

    private static object GetDrawingInfo()
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc == null)
        {
            return new
            {
                drawingName = (string?)null,
                filePath = (string?)null,
                hasActiveDrawing = false
            };
        }

        int entityCount = 0;
        int layerCount = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var lt = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
            foreach (var _ in lt) layerCount++;

            var btr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (var _ in btr) entityCount++;
            tr.Commit();
        }

        return new
        {
            drawingName = Path.GetFileName(doc.Name),
            filePath = doc.Name,
            hasActiveDrawing = true,
            layers = layerCount,
            activeSpaceEntities = entityCount,
            readOnly = doc.IsReadOnly
        };
    }

    private static object GetDrawingSettings()
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc == null) return new { };

        return new
        {
            linearUnits = doc.Database.Lunits.ToString(),
            angularUnits = doc.Database.Aunits.ToString(),
            measurement = doc.Database.Measurement.ToString(),
            currentLayer = doc.Database.Clayer.ToString()
        };
    }

    private static object SaveDrawing(string? saveAsPath)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        if (doc == null) throw new InvalidOperationException("No active document");

        App.DocumentManager.MdiActiveDocument.SendStringToExecute("_.QSAVE ", false, false, false);
        return new { success = true, saved = doc.Name };
    }

    private static void EnsureNodeMcpServerRunning()
    {
        try
        {
            using (var testClient = new TcpClient())
            {
                var result = testClient.BeginConnect("127.0.0.1", DefaultHttpPort, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(300);
                if (success && testClient.Connected)
                {
                    return;
                }
            }
        }
        catch { }

        try
        {
            string indexPath = Path.Combine(SetupDirectory, "build", "index.js");
            if (File.Exists(indexPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "node.exe",
                    Arguments = $"\"{indexPath}\"",
                    WorkingDirectory = SetupDirectory,
                    CreateNoWindow = true,
                    UseShellExecute = true, // Critical: Prevents socket handle inheritance!
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                _nodeProcess = Process.Start(psi);
                LogInfo("Spawned Node.js MCP Server bridge background process.");
            }
        }
        catch (Exception ex)
        {
            LogWarning($"Could not auto-spawn Node MCP server: {ex.Message}");
        }
    }

    private static void LogInfo(string msg)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage($"\n[MCP Server] {msg}");
    }

    private static void LogWarning(string msg)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage($"\n[MCP Server WARNING] {msg}");
    }

    private static void LogError(string msg, Exception ex)
    {
        var doc = App.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage($"\n[MCP Server ERROR] {msg}: {ex.Message}");
    }
}
