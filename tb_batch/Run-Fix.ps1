# Applies tb_fix.lsp in place to the -B drawings: SEAL DATE -> 01/10/26, REVISION NO -> B,
# deletes loose rows overlapping the revision table, strips a stray "-<letter>" from the
# title-block drawing number. Civil 3D keeps a .bak of each saved file.
param([Parameter(Mandatory = $true)][string[]]$Folders, [string]$Filter = "*-B.dwg")

$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_fix_log.csv"
$scr  = Join-Path $here "tb_fix.scr"
$code = (Get-Content (Join-Path $here "tb_audit.lsp")) + (Get-Content (Join-Path $here "tb_fix.lsp")) | Where-Object { $_.Trim() -ne "" }
($code + @("TBFIX", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

$files = foreach ($d in $Folders) { Get-ChildItem -Path $d -Filter $Filter -File }
$files = $files | Sort-Object Name
Write-Host "Fixing $($files.Count) drawing(s)..."
$n = 0
foreach ($f in $files) {
  $n++
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $line = Get-Content $log -ErrorAction SilentlyContinue | Where-Object { $_ -like "$($f.Name),*" } | Select-Object -Last 1
  $status = if ($line) { $line.Split(',')[1] } else { "NO RESULT" }
  $color = if ($status -like "*ERROR*" -or $status -eq "NO RESULT") { "Red" } else { "Green" }
  Write-Host ("  {0,2}/{1} {2,-48} {3}" -f $n, $files.Count, $f.Name, $status) -ForegroundColor $color
}
Write-Host "`nDone. Log: $log"
