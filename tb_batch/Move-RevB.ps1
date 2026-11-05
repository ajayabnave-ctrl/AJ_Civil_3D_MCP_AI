# Moves the Civil Defense -B.dwg files from CAD into CAD\Rev B Files, then rewrites any
# relative raster-image paths in every drawing in Rev B Files to absolute paths pointing at
# CAD\Xref_Images, so logos/legend images keep loading. Existing files are never overwritten.

$cad  = "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD"
$dest = Join-Path $cad "Rev B Files"
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_img_log.csv"
$scr  = Join-Path $here "tb_img.scr"

# 1. Move
New-Item -ItemType Directory -Force -Path $dest | Out-Null
foreach ($f in Get-ChildItem $cad -Filter "*-B.dwg" -File) {
  $target = Join-Path $dest $f.Name
  if (Test-Path $target) { Write-Host "SKIPPED (exists in Rev B Files): $($f.Name)" -ForegroundColor Yellow; continue }
  try { Move-Item $f.FullName $dest -ErrorAction Stop; Write-Host "Moved: $($f.Name)" }
  catch { Write-Host "NOT MOVED (open in Civil 3D?): $($f.Name)" -ForegroundColor Red }
}

# 2. Fix relative image paths (source folder = CAD)
$src = ($cad.TrimEnd('\') + '\') -replace '\\', '\\'
$code = @("(setq *TB-SRCDIR* `"$src`")") + (Get-Content (Join-Path $here "tb_img.lsp") | Where-Object { $_.Trim() -ne "" })
($code + @("TBIMG", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

$files = Get-ChildItem $dest -Filter "*-B.dwg" -File | Sort-Object Name
Write-Host "`nFixing image paths in $($files.Count) drawing(s)..."
foreach ($f in $files) {
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $line = Get-Content $log -ErrorAction SilentlyContinue | Where-Object { $_ -like "$($f.Name),*" } | Select-Object -Last 1
  $status = if ($line) { $line.Split(',')[1] } else { "NO RESULT" }
  Write-Host ("  {0,-48} {1}" -f $f.Name, $status)
}
# Keep the .bak files Civil 3D just made with the other backups
$bk = Join-Path $dest "Backup"; New-Item -ItemType Directory -Force -Path $bk | Out-Null
Get-ChildItem $dest -Filter "*.bak" -File | ForEach-Object {
  $t = Join-Path $bk $_.Name
  if (Test-Path $t) { $t = Join-Path $bk ($_.BaseName + "-before-imgfix.bak") }
  Move-Item $_.FullName $t
}
Write-Host "`nDone."
