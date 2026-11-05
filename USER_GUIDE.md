# AutoCAD 2024 & Civil 3D MCP Integration Guide
**Automated CAD File Updates, Title Block Revisions, and AI Model Context Protocol**

---

## 1. Quick Start

1. **Launch AutoCAD 2024** (or Civil 3D).
2. The add-in loads **automatically** without requiring `NETLOAD`.
3. Locate the **`AI CAD MCP`** tab on your top Ribbon bar.
4. The server starts automatically on port `8080` (TCP) and spawns the Node.js MCP bridge on port `3000` (HTTP).
5. Open your AI client (Claude Code, Claude Desktop, Cursor, etc.) and begin giving drawing commands.

---

## 2. System Architecture

```text
┌────────────────────────────────────────────────────────┐
│     AI Assistant (Claude Code / Desktop / Cursor)      │
└──────────────────────────┬─────────────────────────────┘
                           │ (stdio or HTTP :3000)
┌──────────────────────────▼─────────────────────────────┐
│  Civil 3D / AutoCAD MCP Server (Node.js / TypeScript)  │
│  Location: C:\RevitAddinDev\CIVIL 3D_MCP Setup\build   │
└──────────────────────────┬─────────────────────────────┘
                           │ (JSON-RPC 2.0 TCP :8080)
┌──────────────────────────▼─────────────────────────────┐
│    AutoCAD 2024 MCP Add-in (AutoCAD2024McpAddin.dll)   │
│    Loaded inside AutoCAD 2024 (.NET Framework 4.8)     │
└──────────────────────────┬─────────────────────────────┘
                           │
┌──────────────────────────▼─────────────────────────────┐
│           Active Drawing Database (.dwg)               │
│  Layouts, Blocks, Attributes, Title Blocks, Layers     │
└────────────────────────────────────────────────────────┘
```

---

## 3. Ribbon Controls & In-CAD Commands

The **`AI CAD MCP`** Ribbon tab provides one-click control over the server and title block workflows:

### A. MCP Server Bridge Panel

| Button | Command | Description |
|---|---|---|
| **▶ Start Server** | `C3DMCPSTART` | Starts the in-process TCP server on port `8080` and verifies/spawns the Node.js MCP bridge. |
| **🔄 Refresh Server** | `C3DMCPREFRESH` | Restarts the TCP listener, clears socket connections, and resynchronizes active drawing state. |
| **⏹ Stop Server** | `C3DMCPSTOP` | Shuts down the TCP listener and terminates background Node.js bridge processes. |
| **ℹ Status** | `C3DMCPSTATUS` | Displays active status, uptime, requests processed, and drawing name in a popup dialog and command line. |
| **🗂 Palette UI** | `C3DMCPPALETTE` | Toggles the dark-themed floating / dockable Tool Palette with all action buttons. |

### B. Title Block Automation Panel

| Button | Command | Description |
|---|---|---|
| **📋 Dump Title Block** | `C3DTBDUMP` or `TBDUMP` | Dumps all handles, entity types, layers, text strings, and title block attribute definitions from the active layout into `tb_dump2.txt`. |
| **✏ Update Revision** | `C3DTBREV` or `TBREV` | Prompts for a revision letter (e.g. `B`) and updates the title block revision tags and rows under a single undo step (`U`). |
| **📦 Batch CAD Runner** | `C3DTBBATCH` | Opens the `tb_batch` directory containing scripts for headless folder-level processing with `accoreconsole.exe`. |

---

## 4. Title Block & CAD File Workflows

### Workflow 1: Inspect Title Block Attributes (`TBDUMP`)
To inspect what block attributes, layers, or text exist in your active layout title block:
1. Click **`📋 Dump Title Block`** on the ribbon (or type `TBDUMP`).
2. The add-in generates a structured report at:
   `C:\RevitAddinDev\CIVIL 3D_MCP Setup\tb_dump2.txt`
3. AI assistants can read this file directly to discover block attribute tags (e.g. `REV`, `REVISION`, `DRAWN_BY`, `SCALE`, `DATE`).

### Workflow 2: Update Title Block Revision (`TBREV`)
1. Click **`✏ Update Revision`** on the ribbon (or type `TBREV`).
2. Type the new revision letter when prompted (e.g., `B`, `C`).
3. The add-in scans the active layout, updates revision blocks and attributes, and performs a screen regen.
4. **Undo Protection**: Type `U` at any time to instantly revert the update.

### Workflow 3: Batch Headless Processing (`Run-TitleBlockBatch.ps1`)
To update title blocks across dozens or hundreds of drawings without opening AutoCAD GUI:
1. Open PowerShell and navigate to:
   ```powershell
   cd "C:\RevitAddinDev\CIVIL 3D_MCP Setup\tb_batch"
   ```
2. Run on a single test file:
   ```powershell
   .\Run-TitleBlockBatch.ps1 -InputFolder "D:\Your\CAD\Folder" -Limit 1
   ```
3. Run across the entire directory:
   ```powershell
   .\Run-TitleBlockBatch.ps1 -InputFolder "D:\Your\CAD\Folder"
   ```
* **Safe**: Originals (`*-D.dwg`) are kept intact; processed copies are renamed to the new revision (e.g. `*-B.dwg`).
* **Image Relinking**: Embedded images/logos are automatically remapped to prevent broken xrefs.
* **Audit Logging**: Summary is logged to `tb_batch_summary.csv`.

---

## 5. Connecting AI Clients

### A. Claude Code CLI
Claude Code is already registered globally with user scope.
Verify status anytime by running in PowerShell:
```powershell
claude mcp list
```
Output:
```text
civil3d-mcp: node C:\RevitAddinDev\CIVIL 3D_MCP Setup\build\index.js - ✓ Connected
```

### B. Claude Desktop
Configuration is saved at `%APPDATA%\Claude\claude_desktop_config.json`:
```json
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
```

### C. Cursor / Antigravity / Other IDEs
Project-level configuration is saved at:
`C:\RevitAddinDev\CIVIL 3D_MCP Setup\.mcp.json`

---

## 6. How Auto-Loading Works (No NETLOAD Required)

AutoCAD 2024 automatically loads the add-in via two redundant mechanisms:

1. **AutoCAD 2024 Registry Demand-Load**:
   - Key: `HKCU\Software\Autodesk\AutoCAD\R24.3\ACAD-7101:409\Applications\AutoCAD2024McpAddin`
   - `LOADCTRLS` = 2 (Load on startup)
   - `LOADER` = `C:\RevitAddinDev\CIVIL 3D_MCP Setup\AutoCAD2024-MCP-Addin\bin\Release\net48\AutoCAD2024McpAddin.dll`
   - `MANAGED` = 1
2. **Autodesk ApplicationPlugins Autoloader**:
   - Location: `%APPDATA%\Autodesk\ApplicationPlugins\Civil3DMcpPlugin.bundle\PackageContents.xml`
   - Configured for `SeriesMin="R24.0" SeriesMax="R24.3"` for AutoCAD 2024, and `SeriesMin="R25.0"` for AutoCAD 2026+.

---

## 7. Troubleshooting & FAQ

### Q: What if port 8080 is blocked or already in use?
* In AutoCAD, click **`Stop Server`** and then **`Start Server`** to release and rebind the socket.
* You can check port state in PowerShell:
  ```powershell
  Get-NetTCPConnection -LocalPort 8080 -ErrorAction SilentlyContinue
  ```

### Q: How do I manually start the Node.js MCP server outside AutoCAD?
Double-click:
`C:\RevitAddinDev\CIVIL 3D_MCP Setup\start-server.bat`
Or in PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File "C:\RevitAddinDev\CIVIL 3D_MCP Setup\start-server.ps1"
```

### Q: Where are the logs?
* AutoCAD Command Line prints `[MCP Server]` status messages.
* Title block batch logs are saved in `C:\RevitAddinDev\CIVIL 3D_MCP Setup\tb_batch\tb_batch_log.csv`.
