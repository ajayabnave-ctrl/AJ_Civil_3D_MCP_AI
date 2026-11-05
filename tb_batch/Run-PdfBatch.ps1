# Batch-plots every *-B.dwg (all layouts) to A1 PDF with DWG To PDF.pc3,
# ISO full bleed A1 (841 x 594 mm), landscape, layout 1:1, lineweights on,
# each drawing's own plot style kept. PDF name = drawing file name.
#
# Test first:  .\Run-PdfBatch.ps1 -Limit 1
# All:         .\Run-PdfBatch.ps1
param(
  [string]$Root   = "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD",
  [string]$Out    = "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\PDF\B Rev_Civil Defense_PDFS",
  [string]$Filter = "*-B.dwg",
  [int]$Limit = 0
)
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_pdf_log.csv"
$scr  = Join-Path $here "tb_pdf.scr"
New-Item -ItemType Directory -Force -Path $Out | Out-Null

$outLisp = (($Out.TrimEnd('\')) + '\') -replace '\\', '/'
$code = @("(setq *PDF-DIR* `"$outLisp`" *PDF-LOG* `"$($log -replace '\\','/')`")") +
        (Get-Content (Join-Path $here "tb_pdf.lsp") | Where-Object { $_.Trim() -ne "" })
($code + @("TBPDF")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

$files = Get-ChildItem $Root -Filter $Filter -File -Recurse | Where-Object { $_.DirectoryName -notlike "*\Backup" } | Sort-Object Name
if ($Limit -gt 0) { $files = $files | Select-Object -First $Limit }
Write-Host "Plotting $($files.Count) drawing(s) -> $Out`n"
$ok = 0
foreach ($f in $files) {
  Get-ChildItem $Out -Filter "$($f.BaseName)*.pdf" -ErrorAction SilentlyContinue | Remove-Item
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $pdfs = Get-ChildItem $Out -Filter "$($f.BaseName)*.pdf" -ErrorAction SilentlyContinue
  if ($pdfs) {
    foreach ($p in $pdfs) { Write-Host ("  OK  {0}  ({1:N0} KB)" -f $p.Name, ($p.Length/1KB)) -ForegroundColor Green }
    $ok++
  } else { Write-Host "  NO PDF  $($f.Name)" -ForegroundColor Red }
}
Write-Host "`nDone: $ok of $($files.Count) drawings plotted. Folder: $Out"
