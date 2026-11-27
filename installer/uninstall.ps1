# Remove a Bandeja de Impressoras do usuario atual.
$app  = 'BandejaImpressoras'
$nome = 'Bandeja de Impressoras'
$dest = Join-Path $env:LOCALAPPDATA "Programs\$app"

Get-Process $app -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name $app -ErrorAction SilentlyContinue
Remove-Item "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$app" -Recurse -ErrorAction SilentlyContinue
Remove-Item (Join-Path ([Environment]::GetFolderPath('Programs')) "$nome.lnk") -ErrorAction SilentlyContinue
Remove-Item (Join-Path $env:APPDATA $app) -Recurse -Force -ErrorAction SilentlyContinue   # config.json

Set-Location $env:TEMP
Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "$nome removido."
