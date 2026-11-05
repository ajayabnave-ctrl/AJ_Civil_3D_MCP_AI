# Read-only: lists raster image paths in selected drawings and whether each file is found.
param([string[]]$Files = @(
  "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\PLANS B REV\NC-CQT-566-3804-RTC-DWG-LI-600-0000001-B.dwg",
  "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\PLANS B REV\NC-CQT-566-3804-RTC-DWG-LI-600-0000020-B.dwg",
  "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\NC-CQT-566-3804-RTC-DWG-LI-600-0000020-D.dwg"))
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out  = Join-Path $here "tb_imgdiag.txt"
$scr  = Join-Path $here "tb_imgdiag.scr"
Remove-Item $out -ErrorAction SilentlyContinue
$code = @("(setq *ID-OUT* `"$($out -replace '\\','/')`")") + (Get-Content (Join-Path $here "tb_imgdiag.lsp") | Where-Object { $_.Trim() -ne "" })
($code + @("TBIMGDIAG")) | Set-Content -Path $scr -Encoding ASCII
foreach ($f in $Files) { Write-Host "Checking $f"; & $acc /i "$f" /s "$scr" /product C3D /language en-US | Out-Null }
Write-Host "Done. Results: $out"
