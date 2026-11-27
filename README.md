# Bandeja de Impressoras

Programa para Windows 10/11 que fica **só na bandeja do sistema** e abre a fila de impressão da sua impressora com um clique.

| Ação | O que acontece |
|---|---|
| **Clique esquerdo** no ícone | Abre direto a **fila de trabalhos** da impressora padrão. |
| **Clique direito** no ícone | Abrir a fila da padrão · **Trocar impressora padrão** (lista todas as impressoras instaladas) · **Iniciar com o Windows** · Sair |
| **Primeira execução** | Pergunta qual impressora será a padrão e salva a escolha. |
| **Impressora salva sumiu** | Avisa e pede outra; o programa continua funcionando mesmo se você cancelar. |

> A "impressora padrão" aqui é a preferência do programa. A impressora padrão do Windows não é alterada.

## Instalação

Baixe o **`BandejaImpressoras-x.y.z.msi`** em **[Releases](../../releases)** e dê dois cliques nele.

É um instalador padrão do Windows (Windows Installer). Não precisa de administrador nem de runtime, porque o .NET Framework 4.8 já vem no Windows 10/11. O instalador:

- instala o programa em `C:\Atlas\bandeja-impressoras`, criando a pasta se ela não existir;
- abre o programa ao terminar.

"Iniciar com o Windows" fica desligado depois de instalar. Ligue pelo menu do clique direito.

O programa aparece em *Configurações › Aplicativos › Aplicativos instalados*, de onde é desinstalado. Para atualizar, basta instalar o `.msi` da versão nova: ele fecha o programa e substitui a versão anterior.

Instalação silenciosa (para scripts de logon, GPO ou ferramentas de distribuição):

```powershell
msiexec /i BandejaImpressoras-x.y.z.msi /qn
```

## Antivírus e SmartScreen

O executável não é assinado digitalmente.

- **SmartScreen:** só avisa sobre arquivos baixados da internet. Para distribuir internamente sem aviso, coloque o `.msi` em uma pasta compartilhada da rede (`\\servidor\apps\...`, usando o nome curto do servidor) ou desbloqueie-o depois de baixar: *Propriedades › Desbloquear*.
- **Antivírus corporativo:** a primeira versão instalava por scripts PowerShell, e o Advanced Threat Control do Bitdefender bloqueou o PowerShell por "comportamento suspeito" (`ATC.SuspiciousBehavior`). O instalador `.msi` não usa PowerShell. Se uma máquina já registrou o bloqueio, o antivírus pode continuar barrando a entrada `BandejaImpressoras` em `HKCU\...\Run` que o instalador antigo criou. Nesse caso, "Iniciar com o Windows" mostra um aviso e não liga. Nesse caso, a TI precisa liberar essa entrada no console do antivírus.

Se o ícone não aparecer, ele está em "ícones ocultos" (a setinha ^ da barra de tarefas). Para deixá-lo sempre visível, ative-o em *Configurações › Personalização › Barra de tarefas › Outros ícones da bandeja do sistema*.

## Como funciona

Tudo usa APIs nativas do Windows, sem software de terceiros:

- **Listagem:** `EnumPrinters` (winspool.drv), nível 4, com impressoras locais e conexões de rede. O nível 4 só lê o registro, então não trava quando uma impressora de rede está desligada.
- **Fila:** `rundll32 printui.dll,PrintUIEntry /o /n "<impressora>"` abre a janela nativa "Ver o que está sendo impresso".
- **Configuração:** `%APPDATA%\BandejaImpressoras\config.json`.
- **Iniciar com o Windows:** valor `BandejaImpressoras` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

Todo o código está em [`src/BandejaImpressoras/Program.cs`](src/BandejaImpressoras/Program.cs).

## Compilar

Requer o [.NET SDK](https://dotnet.microsoft.com/download) (6 ou superior). O [WiX Toolset 5](https://wixtoolset.org/), que gera o `.msi`, é baixado automaticamente pelo NuGet.

```powershell
.\build.cmd 1.0.0
```

O resultado fica em `dist\BandejaImpressoras-1.0.0.msi`. O instalador está definido em [`installer/Package.wxs`](installer/Package.wxs).

> Se a compilação falhar com "não foi possível copiar ... withSupportedRuntime.config", o caminho da pasta é longo demais (mais de 260 caracteres). Clone o repositório em um caminho mais curto, como `C:\src`.

## Publicar uma versão

O GitHub Actions compila a cada push. Ao enviar uma tag `v*`, ele também cria a Release com o `.msi`:

```powershell
git tag v1.0.1
git push origin v1.0.1
```
