# Instala a Bandeja de Impressoras para o usuario atual (sem precisar de administrador).
#   - copia o programa para %LOCALAPPDATA%\Programs\BandejaImpressoras
#   - cria atalho no Menu Iniciar
#   - registra em "Configuracoes > Aplicativos" (para desinstalar por la)
#   - ativa "Iniciar com o Windows" (pode ser desligado pelo menu da bandeja)
param([switch]$SemIniciarComWindows)

$ErrorActionPreference = 'Stop'
$app  = 'BandejaImpressoras'
$nome = 'Bandeja de Impressoras'
$dest = Join-Path $env:LOCALAPPDATA "Programs\$app"
$exe  = Join-Path $dest "$app.exe"

$origem = Join-Path $PSScriptRoot "$app.exe"
if (-not (Test-Path $origem)) { throw "Arquivo nao encontrado: $origem" }

# Fecha a versao em execucao (atualizacao).
Get-Process $app -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item $origem $exe -Force
Copy-Item (Join-Path $PSScriptRoot 'uninstall.ps1') $dest -Force
Get-ChildItem $dest | Unblock-File   # remove a marca "baixado da internet"

# Atalho no Menu Iniciar
$atalho = Join-Path ([Environment]::GetFolderPath('Programs')) "$nome.lnk"
$sh = New-Object -ComObject WScript.Shell
$lnk = $sh.CreateShortcut($atalho)
$lnk.TargetPath = $exe
$lnk.WorkingDirectory = $dest
$lnk.Description = $nome
$lnk.Save()

# Entrada em Configuracoes > Aplicativos > Aplicativos instalados
$versao = (Get-Item $exe).VersionInfo.ProductVersion
$chave = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$app"
New-Item $chave -Force | Out-Null
$props = @{
    DisplayName     = $nome
    DisplayVersion  = $versao
    DisplayIcon     = $exe
    InstallLocation = $dest
    Publisher       = $nome
    UninstallString = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$dest\uninstall.ps1`""
}
foreach ($p in $props.GetEnumerator()) { Set-ItemProperty $chave -Name $p.Key -Value $p.Value }
Set-ItemProperty $chave -Name NoModify -Value 1 -Type DWord
Set-ItemProperty $chave -Name NoRepair -Value 1 -Type DWord

# Iniciar com o Windows (mesmo valor que o programa grava pelo menu)
$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if ($SemIniciarComWindows) { Remove-ItemProperty $run -Name $app -ErrorAction SilentlyContinue }
else { Set-ItemProperty $run -Name $app -Value "`"$exe`"" }

Start-Process $exe
Write-Host "$nome $versao instalado em $dest"
