param(
    [ValidateSet("8080", "8081", "8082")]
    [string]$Port = "8080"
)

Write-Host "Configuring Civil 3D / AutoCAD MCP Port to: $Port" -ForegroundColor Cyan

# 1. Update server_config.json for AutoCAD addin
$setupDir = "C:\RevitAddinDev\CIVIL 3D_MCP Setup"
$cfg = @{ port = [int]$Port }
$cfg | ConvertTo-Json | Set-Content (Join-Path $setupDir "server_config.json") -Encoding UTF8
Write-Host "  [OK] Updated $setupDir\server_config.json" -ForegroundColor Green

# 2. Update Claude Desktop config
$claudeDesktopCfg = "$env:APPDATA\Claude\claude_desktop_config.json"
if (Test-Path $claudeDesktopCfg) {
    try {
        $json = Get-Content $claudeDesktopCfg -Raw | ConvertFrom-Json
        if ($json.mcpServers."civil3d-mcp") {
            $json.mcpServers."civil3d-mcp".env.CIVIL3D_PORT = $Port
            $json | ConvertTo-Json -Depth 10 | Set-Content $claudeDesktopCfg -Encoding UTF8
            Write-Host "  [OK] Updated Claude Desktop config ($claudeDesktopCfg)" -ForegroundColor Green
        }
    } catch {
        Write-Host "  [WARN] Failed to update Claude Desktop config: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# 3. Update project .mcp.json
$projectMcp = Join-Path $setupDir ".mcp.json"
if (Test-Path $projectMcp) {
    try {
        $json = Get-Content $projectMcp -Raw | ConvertFrom-Json
        if ($json.mcpServers."civil3d-mcp") {
            $json.mcpServers."civil3d-mcp".env.CIVIL3D_PORT = $Port
            $json | ConvertTo-Json -Depth 10 | Set-Content $projectMcp -Encoding UTF8
            Write-Host "  [OK] Updated Project .mcp.json" -ForegroundColor Green
        }
    } catch {}
}

# 4. Update Claude Code config (~/.claude.json)
$claudeCodeCfg = "$env:USERPROFILE\.claude.json"
if (Test-Path $claudeCodeCfg) {
    try {
        $json = Get-Content $claudeCodeCfg -Raw | ConvertFrom-Json
        if ($json.mcpServers."civil3d-mcp") {
            $json.mcpServers."civil3d-mcp".env.CIVIL3D_PORT = $Port
            $json | ConvertTo-Json -Depth 10 | Set-Content $claudeCodeCfg -Encoding UTF8
            Write-Host "  [OK] Updated Claude Code user config ($claudeCodeCfg)" -ForegroundColor Green
        }
    } catch {}
}

# 5. Check if chosen port is in use
$conn = Get-NetTCPConnection -LocalPort ([int]$Port) -ErrorAction SilentlyContinue
if ($conn) {
    $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
    Write-Host "  [INFO] Port $Port is currently in use by process: $($proc.ProcessName) (PID $($conn.OwningProcess))" -ForegroundColor Cyan
} else {
    Write-Host "  [INFO] Port $Port is currently free and available." -ForegroundColor Yellow
}

Write-Host "`nCompleted! If Claude Desktop is open, restart it to apply the new port." -ForegroundColor White
