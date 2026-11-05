# Read-only: dumps model-space text/tables of both sheet-list drawings (LI-100-1000002-B).
$roots = "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD",
         "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD"
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$files = Get-ChildItem $roots -Filter "*-100-1000002-B.dwg" -File -Recurse | Where-Object { $_.DirectoryName -notlike "*\Backup" }
foreach ($f in $files) {
  $proj = if ($f.Name -like "*-3801-*") { "3801" } else { "3804" }
  $out = (Join-Path $here "tb_ms_$proj.txt") -replace '\\', '/'
  $scr = Join-Path $here "tb_diag_ms.scr"
  $code = @("(setq *DM-OUT* `"$out`")") + (Get-Content (Join-Path $here "tb_diag_ms.lsp") | Where-Object { $_.Trim() -ne "" })
  ($code + @("TBDIAGMS")) | Set-Content -Path $scr -Encoding ASCII
  Write-Host "Dumping $($f.FullName)"
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
}
Write-Host "Done."
