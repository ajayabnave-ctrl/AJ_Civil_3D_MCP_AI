@echo off
title Civil 3D MCP Server
cd /d "%~dp0"
echo Starting Civil 3D MCP Server...
node build/index.js
pause
