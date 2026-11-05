# Changes the drawing-number entries in both sheet lists (model space of LI-100-1000002-B)
# from "...-D" to "...-B". Saves in place; the .bak goes to that folder's Backup subfolder.
$roots = "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD",
         "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD"
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_sheetlist_log.csv"
$scr  = Join-Path $here "tb_sheetlist.scr"
$code = @("(setq *SL-LOG* `"$($log -replace '\\','/')`")") + (Get-Content (Join-Path $here "tb_sheetlist.lsp") | Where-Object { $_.Trim() -ne "" })
($code + @("TBSHEETLIST", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

$files = Get-ChildItem $roots -Filter "*-100-1000002-B.dwg" -File -Recurse | Where-Object { $_.DirectoryName -notlike "*\Backup" }
foreach ($f in $files) {
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $line = Get-Content $log -ErrorAction SilentlyContinue | Where-Object { $_ -like "$($f.Name),*" } | Select-Object -Last 1
  Write-Host ("{0}  {1}" -f $f.Name, $(if ($line) { $line.Split(',')[1] } else { "NO RESULT" }))
  $bak = Join-Path $f.DirectoryName ($f.BaseName + ".bak")
  if (Test-Path $bak) {
    $bk = Join-Path $f.DirectoryName "Backup"; New-Item -ItemType Directory -Force -Path $bk | Out-Null
    $t = Join-Path $bk ($f.BaseName + ".bak"); if (Test-Path $t) { $t = Join-Path $bk ($f.BaseName + "-before-sheetlist.bak") }
    Move-Item $bak $t
  }
}
Write-Host "Done."
