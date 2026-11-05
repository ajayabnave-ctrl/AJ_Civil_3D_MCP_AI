# Civil 3D MCP & Title Block CAD Automation Deployment

This directory (**C:\RevitAddinDev\CIVIL 3D_MCP Setup**) contains the fully deployed and configured **Civil 3D MCP (Model Context Protocol) Server**, the native **Civil 3D C# Plugin**, and the **Title Block & CAD File Automation Suite**.

---

## 1. System Architecture

`	ext
Claude / AI Assistant (Claude Code, Claude Desktop, Cursor, Antigravity)
       │
       ▼ (stdio or HTTP :3000)
Civil 3D MCP Server (Node.js / TypeScript @ C:\RevitAddinDev\CIVIL 3D_MCP Setup)
       │
       ▼ (JSON-RPC TCP :8080)
Civil3DMcpPlugin.dll (.NET 8 Plugin loaded inside Civil 3D / AutoCAD 2026)
       │
       ▼
Autodesk Civil 3D 2026 / AutoCAD Document & Database Runtime
`

In addition, **AutoCAD Core Console (ccoreconsole.exe)** powers headless batch operations for Title Block updates without requiring manual GUI interactions.

---

## 2. What Was Deployed

| Component | Path | Status |
|---|---|---|
| **MCP Server Core** | C:\RevitAddinDev\CIVIL 3D_MCP Setup\build\index.js | Built & verified |
| **Civil 3D .NET Plugin** | C:\RevitAddinDev\CIVIL 3D_MCP Setup\Civil3D-MCP-Plugin\bin\Release\net8.0-windows\Civil3DMcpPlugin.dll | Compiled (.NET 8) |
| **Autodesk Autoloader Bundle** | %APPDATA%\Autodesk\ApplicationPlugins\Civil3DMcpPlugin.bundle | Deployed (Auto-loads on startup) |
| **Claude Code MCP** | Configured in ~/.claude.json (scope: user) | Connected (✓ Connected) |
| **Claude Desktop Config** | %APPDATA%\Claude\claude_desktop_config.json | Configured |
| **Project MCP Config** | C:\RevitAddinDev\CIVIL 3D_MCP Setup\.mcp.json | Configured |
| **Title Block Automation** | C:\RevitAddinDev\CIVIL 3D_MCP Setup\tb_batch\ | Paths calibrated & ready |
| **Title Block LISP Tools** | 	b_dump.lsp, 	b_rev_update.lsp | Paths updated to target folder |

---

## 3. Title Block & CAD Update Capabilities

### A. Live Title Block Management via MCP
AI assistants connected through the MCP server can execute operations through the civil3d_plan_production tool domain:
- sheet_set_title_block: Updates title block references and properties on sheets.
- sheet_get_properties: Inspects sheet metadata, scale, title block block names.
- sheet_publish_pdf: Automates PDF export from layout sheets.
- sheet_set_list / sheet_set_get_info: Manages AutoCAD Sheet Sets (.dst).

### B. Title Block Inspection (	b_dump.lsp)
- Command: TBDUMP
- Scans the active layout's block table and dumps all handles, layers, text entities, MText, and attribute definitions to C:\RevitAddinDev\CIVIL 3D_MCP Setup\tb_dump2.txt.

### C. Single-Drawing Revision Update (	b_rev_update.lsp)
- Command: TBREV
- Reorders revision rows, updates revision letters, and modifies title block revision tags inside a single undo transaction (U).

### D. Headless Batch CAD Processing (	b_batch\Run-TitleBlockBatch.ps1)
Runs through directories of CAD drawings using ccoreconsole.exe:
- **Safe**: Originals (*-D.dwg) are preserved; changes are written to new revision names (e.g. *-B.dwg).
- **Image Relinking**: Automatically maintains or converts relative raster image paths to prevent broken xrefs/logos.
- **Audit & Fix**:
  - Run-Audit.ps1 -> Logs title block attributes across drawings to 	b_audit.csv.
  - Run-Fix.ps1 -> Applies batch fixes to text, layers, and attributes.
  - Run-PdfBatch.ps1 -> Headless batch PDF plotting.

Example execution:
`powershell
cd "C:\RevitAddinDev\CIVIL 3D_MCP Setup\tb_batch"
# Test on 1 drawing
.\Run-TitleBlockBatch.ps1 -InputFolder "D:\Project\CAD" -Limit 1
# Run full batch
.\Run-TitleBlockBatch.ps1 -InputFolder "D:\Project\CAD"
`

---

## 4. Connecting AI Assistants

### Claude Code CLI
Already registered! Verify status anytime by running:
`powershell
claude mcp list
`

### Claude Desktop
Configuration has been saved to %APPDATA%\Claude\claude_desktop_config.json:
`json
{
  "mcpServers": {
    "civil3d-mcp": {
      "command": "node",
      "args": ["C:/RevitAddinDev/CIVIL 3D_MCP Setup/build/index.js"],
      "env": {
        "CIVIL3D_HOST": "localhost",
        "CIVIL3D_PORT": "8080",
        "CIVIL3D_ENABLE_TOOL_ALIASES": "true",
        "CIVIL3D_LOG_LEVEL": "info"
      }
    }
  }
}
`

### Starting Standalone MCP Server Manually
Double-click start-server.bat or run:
`powershell
powershell -ExecutionPolicy Bypass -File "C:\RevitAddinDev\CIVIL 3D_MCP Setup\start-server.ps1"
`
Starts on stdio and activates the local HTTP bridge at http://127.0.0.1:3000.

---

## 5. Autodesk Civil 3D Plugin Management

The plugin starts a JSON-RPC TCP server on localhost:8080.

- **Automatic Loading**: The Autoloader bundle is active at %APPDATA%\Autodesk\ApplicationPlugins\Civil3DMcpPlugin.bundle. Civil 3D 2026/2025 automatically loads the plugin on launch.
- **Manual Loading**: If needed, run NETLOAD inside Civil 3D and browse to:
  C:\RevitAddinDev\CIVIL 3D_MCP Setup\Civil3D-MCP-Plugin\bin\Release\net8.0-windows\Civil3DMcpPlugin.dll
- **In-CAD Commands**:
  - C3DMCPSTATUS: Check listener health, queue status, active operations.
  - C3DMCPSTART: Manually restart listener.
  - C3DMCPSTOP: Stop listener.

---

## 6. AutoCAD 2024 Dedicated Add-in & Ribbon UI

A dedicated add-in (**AutoCAD2024McpAddin.dll**) compiled specifically for **AutoCAD 2024 (.NET Framework 4.8)** has been deployed:

### A. Automatic Startup (No NETLOAD Required)
1. **Autodesk Autoloader Bundle**: Deployed to %APPDATA%\Autodesk\ApplicationPlugins\Civil3DMcpPlugin.bundle\PackageContents.xml. Automatically loads on AutoCAD 2024 launch.
2. **AutoCAD 2024 Registry Demand-Load**: Registered under HKCU\Software\Autodesk\AutoCAD\R24.3\ACAD-7101:409\Applications\AutoCAD2024McpAddin with LOADCTRLS = 2.
3. **Task-Based Initialization**: The server starts automatically when AutoCAD launches or when drawings open.

### B. Ribbon Controls ("AI CAD MCP" Tab)
- **Start Server** (▶): Starts the TCP JSON-RPC server on port 8080 and automatically spawns the background Node.js MCP server (port 3000) if not running.
- **Refresh Server** (🔄): Restarts the server, clears connection pools, and re-syncs active drawing context.
- **Stop Server** (⏹): Stops the TCP listener and terminates background Node.js bridge processes.
- **Server Status** (ℹ): Displays real-time uptime, request count, listener state, and drawing name in a popup dialog.
- **Palette UI** (🗂): Opens a floating dockable WPF tool palette (C3DMCPPALETTE).
- **Dump Title Block** (📋): Extracts all handles, text, blocks, and attribute definitions from active layout into 	b_dump2.txt.
- **Update Revision** (✏): Interactive prompt to update revision letter and attributes in active layout title block.
- **Batch CAD Runner** (📦): Quick access to 	b_batch\Run-TitleBlockBatch.ps1 for batch folder processing.
