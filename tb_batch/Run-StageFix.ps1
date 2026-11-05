# Changes PROJECT STAGE "...SCHEMATIC DESIGN" -> "100% DETAILED DESIGN" in place.
# Revision-table rows (anything above the REV header) are left alone.
# Civil 3D keeps a .bak of each file it saves.
#
# Usage:
#   .\Run-StageFix.ps1 -Folder "D:\...\CAD" -Filter "*-600-000002?-B.dwg"

param(
  [Parameter(Mandatory = $true)][string]$Folder,
  [string]$Filter = "*-B.dwg"
)

$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_stage_log.csv"
$scr  = Join-Path $here "tb_stage.scr"

$code = Get-Content (Join-Path $here "tb_stage.lsp") | Where-Object { $_.Trim() -ne "" }
($code + @("TBSTAGE", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII

$files = Get-ChildItem -Path $Folder -Filter $Filter -File | Sort-Object Name
Write-Host "Processing $($files.Count) drawing(s)`n"
foreach ($f in $files) {
  Write-Host ("  {0,-50}" -f $f.Name) -NoNewline
  Remove-Item $log -ErrorAction SilentlyContinue
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $line = if (Test-Path $log) { Get-Content $log | Select-Object -Last 1 } else { $null }
  if (-not $line) { Write-Host " NO RESULT" -ForegroundColor Red; continue }
  $status = $line.Split(',')[1]
  $color = if ($status -eq "CHANGED=0") { "Yellow" } elseif ($status -like "CHANGED=*") { "Green" } else { "Red" }
  Write-Host " $status" -ForegroundColor $color
}
Write-Host "`nDone."
