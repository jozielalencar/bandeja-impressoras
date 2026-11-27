@echo off
rem Compila o programa e gera o instalador: dist\BandejaImpressoras-<versao>.msi
rem Uso: build.cmd [versao]      (ex.: build.cmd 1.0.2)
setlocal
set VERSAO=%~1
if "%VERSAO%"=="" set VERSAO=1.0.0
cd /d "%~dp0"
if exist dist rmdir /s /q dist
dotnet build installer\BandejaImpressoras.wixproj -c Release -p:Version=%VERSAO% -o dist || exit /b 1
echo.
echo Instalador: %~dp0dist\BandejaImpressoras-%VERSAO%.msi
