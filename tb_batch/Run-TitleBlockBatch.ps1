# Batch title block revision update with AutoCAD Core Console (Civil 3D 2026)
#
# Originals (*-D.dwg) are never modified. Each drawing is copied to a temp name in
# -OutputFolder, processed there, then renamed so the trailing "-<REV>" matches the
# new revision (e.g. ...-0104200-D.dwg -> ...-0104200-B.dwg).
#
# Default OutputFolder = InputFolder. If a different OutputFolder is given, relative
# raster image paths in the saved copies are rewritten to absolute paths so they still load.
#
# Usage:
#   .\Run-TitleBlockBatch.ps1 -InputFolder "D:\...\CAD" -Limit 1     # test on 1 file
#   .\Run-TitleBlockBatch.ps1 -InputFolder "D:\...\CAD"              # all files

param(
  [Parameter(Mandatory = $true)][string]$InputFolder,
  [string]$OutputFolder = $InputFolder,
  [string]$Filter = "*-D.dwg",
  [int]$Limit = 0
)

$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$lsp  = (Join-Path $here "tb_rev_batch.lsp") -replace '\\', '/'
$log  = Join-Path $here "tb_batch_log.csv"
$scr  = Join-Path $here "tb_rev_batch.scr"

if (-not (Test-Path $acc)) { throw "Core Console not found at $acc" }
New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null

# LISP is inlined into the script (no (load ...)), so no Trusted Location is needed.
# Blank lines are dropped because a blank line in a .scr repeats the last command.
$code = Get-Content (Join-Path $here "tb_rev_batch.lsp") | Where-Object { $_.Trim() -ne "" }
# When saving to a different folder, relative image paths would break: pass the source
# folder so the LISP rewrites relative image paths to absolute ones (originals untouched).
$inFull  = (Resolve-Path $InputFolder).Path.TrimEnd('\')
$outFull = (Resolve-Path $OutputFolder).Path.TrimEnd('\')
if ($inFull -ne $outFull) {
  $srcLisp = ($inFull + '\') -replace '\\', '\\'
  $code = @("(setq *TB-SRCDIR* nil)") + ($code | Where-Object { $_ -ne "(setq *TB-SRCDIR* nil)" }) + @("(setq *TB-SRCDIR* `"$srcLisp`")")
}
($code + @("TBREVB", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII

$files = Get-ChildItem -Path $InputFolder -Filter $Filter -File | Sort-Object Name
if ($Limit -gt 0) { $files = $files | Select-Object -First $Limit }
Write-Host "Processing $($files.Count) drawing(s) -> $OutputFolder`n"

$ok = 0; $warn = 0
foreach ($f in $files) {
  $tmpName = "$($f.BaseName).__tbtmp.dwg"
  $tmp = Join-Path $OutputFolder $tmpName
  Write-Host ("  {0,-50}" -f $f.Name) -NoNewline

  Remove-Item $log -ErrorAction SilentlyContinue   # fresh per-drawing result
  Copy-Item $f.FullName $tmp -Force
  & $acc /i "$tmp" /s "$scr" /product C3D /language en-US | Out-Null
  Remove-Item (Join-Path $OutputFolder "$($f.BaseName).__tbtmp.bak") -ErrorAction SilentlyContinue

  $line = $null
  if (Test-Path $log) { $line = Get-Content $log | Where-Object { $_ -like "*$tmpName,*" } | Select-Object -Last 1 }
  if (-not $line) {
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    Write-Host " NO RESULT - script did not run (temp removed)" -ForegroundColor Red
    $warn++; continue
  }

  Add-Content -Path (Join-Path $here "tb_batch_summary.csv") -Value "$($f.Name),$($line.Split(',')[1])"
  $parts = $line.Split(','); $status = $parts[1]; $rev = $parts[2]
  if (-not $rev) {
    Remove-Item $tmp -Force
    Write-Host " $status (no change, temp removed)" -ForegroundColor Yellow
    $warn++; continue
  }

  $newName = ($f.BaseName -replace '-[A-Z]$', "-$rev") + ".dwg"
  if ($newName -eq $f.Name) { $newName = "$($f.BaseName)-$rev.dwg" }
  $target = Join-Path $OutputFolder $newName
  if (Test-Path $target) {
    Write-Host " $status but $newName already exists - left as $tmpName" -ForegroundColor Yellow
    $warn++; continue
  }
  Rename-Item -Path $tmp -NewName $newName
  Write-Host " $status -> $newName" -ForegroundColor Green
  $ok++
}
Write-Host "`nDone: $ok updated, $warn need attention. Log: $log"
