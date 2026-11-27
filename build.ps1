# Compila e gera o pacote de instalacao: dist\BandejaImpressoras-<versao>.zip
param([string]$Versao = '1.0.0')

$ErrorActionPreference = 'Stop'
$raiz = $PSScriptRoot
$bin  = Join-Path $raiz 'bin'
$pkg  = Join-Path $raiz 'dist\pacote'
$zip  = Join-Path $raiz "dist\BandejaImpressoras-$Versao.zip"

Remove-Item $bin, (Join-Path $raiz 'dist') -Recurse -Force -ErrorAction SilentlyContinue
dotnet build (Join-Path $raiz 'src\BandejaImpressoras\BandejaImpressoras.csproj') -c Release -o $bin "-p:Version=$Versao"
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilacao' }

New-Item -ItemType Directory -Force $pkg | Out-Null
Copy-Item (Join-Path $bin 'BandejaImpressoras.exe') $pkg
Copy-Item (Join-Path $raiz 'installer\*') $pkg
Compress-Archive -Path (Join-Path $pkg '*') -DestinationPath $zip -Force
Write-Host "Pacote: $zip"
