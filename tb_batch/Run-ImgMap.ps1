# Relinks every raster image that can't be found to the matching file (by image type:
# DCLogo / Egislogo / RTCC / LVI / legend symbol mark) in the Post Office Xref Images folder.
# Images that already load are not touched. Saves in place; .bak -> Backup.
param(
  [string]$Folder = "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\PLANS B REV",
  [string]$ImgDir = "D:\Lighting Design\DD100_Post Office 566-3804_LI_Package\Layouts\CAD\Xref Images",
  [string]$Filter = "*-B.dwg"
)
$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_imgmap_log.csv"
$scr  = Join-Path $here "tb_imgmap.scr"

$imgs = Get-ChildItem $ImgDir -File | Where-Object { $_.Extension -match '\.(png|jpg|jpeg|bmp|tif|tiff)$' }
$filesLisp = ($imgs | ForEach-Object { '"' + $_.Name + '"' }) -join ' '
$dirLisp = (($ImgDir.TrimEnd('\')) + '\') -replace '\\', '\\'
$code = @("(setq *IM-LOG* `"$($log -replace '\\','/')`" *IM-DIR* `"$dirLisp`" *IM-FILES* (list $filesLisp))") +
        (Get-Content (Join-Path $here "tb_imgmap.lsp") | Where-Object { $_.Trim() -ne "" })
($code + @("TBIMGMAP", "_.QSAVE")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

foreach ($f in (Get-ChildItem $Folder -Filter $Filter -File | Sort-Object Name)) {
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
  $line = Get-Content $log -ErrorAction SilentlyContinue | Where-Object { $_ -like "$($f.Name),*" } | Select-Object -Last 1
  Write-Host ("{0,-50} {1}" -f $f.Name, $(if ($line) { $line.Split(',')[1] } else { "NO RESULT" }))
  $bak = Join-Path $f.DirectoryName ($f.BaseName + ".bak")
  if (Test-Path $bak) {
    $bk = Join-Path $f.DirectoryName "Backup"; New-Item -ItemType Directory -Force -Path $bk | Out-Null
    $t = Join-Path $bk ($f.BaseName + "-before-imgmap.bak"); Move-Item $bak $t -Force
  }
}
Write-Host "Done. Details (unmatched image names): $log"
