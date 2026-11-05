$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot
Write-Host "Starting Civil 3D MCP Server on stdio & HTTP 127.0.0.1:3000..." -ForegroundColor Cyan
& node "build/index.js"
