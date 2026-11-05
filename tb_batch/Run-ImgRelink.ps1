# Relinks raster images whose saved path can't be found, by file name, to the local
# "Xref Images" folders. Only broken paths are changed. Saves in place; .bak -> Backup.
param(
  [string[]]$Folders = @(
    "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\PLANS B REV",
    "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD\Rev B Files",
    "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD"),
  [string]$Filter = "*-B.dwg"
)
$searchDirs = @(
  "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\Xref Images\",
  "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD\Xref_Images\",
  "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\",
  "D:\Lighting Design\566-3801_DD100_Civil Defense Package_DT-02-10-2026\Layouts\CAD\")
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_imgrelink_log.csv"
$scr  = Join-Path $here "tb_imgrelink.scr"
$dirsLisp = ($searchDirs | ForEach-Object { '"' + ($_ -replace '\\','\\') + '"' }) -join ' '
$code = @("(setq *IR-LOG* `"$($log -replace '\\','/')`" *IR-DIRS* (list $dirsLisp))") +
        (Get-Content (Join-Path $here "tb_imgrelink.lsp") | Where-Object { $_.Trim() -ne "" })
($code + @("TBIMGRELINK", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

$files = foreach ($d in $Folders) { if (Test-Path $d) { Get-ChildItem $d -Filter $Filter -File } }
foreach ($f in ($files | Sort-Object FullName -Unique)) {
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $line = Get-Content $log -ErrorAction SilentlyContinue | Where-Object { $_ -like "$($f.Name),*" } | Select-Object -Last 1
  Write-Host ("{0,-50} {1}" -f $f.Name, $(if ($line) { $line.Split(',')[1] } else { "NO RESULT" }))
  $bak = Join-Path $f.DirectoryName ($f.BaseName + ".bak")
  if (Test-Path $bak) {
    $bk = Join-Path $f.DirectoryName "Backup"; New-Item -ItemType Directory -Force -Path $bk | Out-Null
    $t = Join-Path $bk ($f.BaseName + ".bak"); if (Test-Path $t) { $t = Join-Path $bk ($f.BaseName + "-before-relink.bak") }
    Move-Item $bak $t -Force
  }
}
Write-Host "Done."
