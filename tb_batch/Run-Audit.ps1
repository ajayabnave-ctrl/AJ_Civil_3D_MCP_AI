# Read-only audit of title blocks (REVISION NO, SEAL DATE, other dates, revision-table overlays).
# Nothing is saved. Results: tb_audit.csv in this folder.
param([Parameter(Mandatory = $true)][string[]]$Folders, [string]$Filter = "*-B.dwg")

$acc  = "C:\Program Files\Autodesk\AutoCAD 2026\accoreconsole.exe"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$log  = Join-Path $here "tb_audit.csv"
$scr  = Join-Path $here "tb_audit.scr"
$code = Get-Content (Join-Path $here "tb_audit.lsp") | Where-Object { $_.Trim() -ne "" }
($code + @("TBAUDIT")) | Set-Content -Path $scr -Encoding ASCII
Remove-Item $log -ErrorAction SilentlyContinue

$files = foreach ($d in $Folders) { Get-ChildItem -Path $d -Filter $Filter -File }
$files = $files | Sort-Object Name
Write-Host "Auditing $($files.Count) drawing(s)..."
$n = 0
foreach ($f in $files) {
  $n++; Write-Host ("  [{0}/{1}] {2}" -f $n, $files.Count, $f.Name)
  & $acc /i "$($f.FullName)" /s "$scr" /product C3D /language en-US | Out-Null
}
Write-Host "`nDone. Results: $log"
