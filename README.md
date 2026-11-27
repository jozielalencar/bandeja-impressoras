# Bandeja de Impressoras

Programa para Windows 10/11 que fica **só na bandeja do sistema** e abre a fila de impressão de qualquer impressora com dois cliques.

| Ação | O que acontece |
|---|---|
| **Clique esquerdo** no ícone | Lista todas as impressoras instaladas. Ao escolher uma, abre a **fila de trabalhos** dela e ela passa a ser a padrão (marcada em negrito). |
| **Clique direito** no ícone | Abrir a fila da padrão · **Trocar impressora padrão** · **Iniciar com o Windows** · Sair |
| **Primeira execução** | Pergunta qual impressora será a padrão e salva a escolha. |
| **Impressora salva sumiu** | Avisa e pede outra; o programa continua funcionando mesmo se você cancelar. |

> A "impressora padrão" aqui é a preferência do programa. A impressora padrão do Windows não é alterada.

## Instalação

1. Baixe o `BandejaImpressoras-x.y.z.zip` em **[Releases](../../releases)** e extraia.
2. Dê dois cliques em **`Instalar.cmd`**.

Não precisa de administrador nem de runtime: o .NET Framework 4.8 já vem no Windows 10/11. O instalador:

- copia o programa para `%LOCALAPPDATA%\Programs\BandejaImpressoras`;
- cria um atalho no Menu Iniciar;
- registra o programa em *Configurações › Aplicativos › Aplicativos instalados*, de onde ele também pode ser desinstalado;
- ativa "Iniciar com o Windows". Para não ativar, use `Instalar.cmd -SemIniciarComWindows`.

Para atualizar, rode o `Instalar.cmd` da versão nova. Se a mesma versão já estiver instalada, ele não faz nada (use `-Forcar` para reinstalar). Para remover, use *Aplicativos instalados* ou o `Desinstalar.cmd`.

## Distribuição interna (sem SmartScreen)

O executável não é assinado. Mesmo assim, o programa **instalado** não dispara o SmartScreen, porque o instalador remove do `.exe` a marca de "baixado da internet". O único aviso possível aparece quando alguém executa o `Instalar.cmd` a partir de um `.zip` baixado. Para evitar até esse aviso:

- **Pasta compartilhada:** extraia o `.zip` em um compartilhamento da rede, como `\\servidor\apps\BandejaImpressoras`, e rode o `Instalar.cmd` de lá. Use o nome curto do servidor: endereços com ponto (IP ou FQDN) são tratados como Internet, a menos que você os adicione à zona *Intranet local* por GPO.
- **Ou** desbloqueie o `.zip` antes de extrair: `Unblock-File .\BandejaImpressoras-x.y.z.zip`.

**Instalação em várias máquinas:** a instalação é por usuário. Configure o `Instalar.cmd` da pasta compartilhada como **script de logon** por GPO (*Configuração do Usuário › Políticas › Configurações do Windows › Scripts › Logon*). Se a versão já estiver instalada, o script termina na hora. Para atualizar todo mundo, basta trocar os arquivos na pasta compartilhada.

Se o ícone não aparecer, ele está em "ícones ocultos" (a setinha ^ da barra de tarefas). Para deixá-lo sempre visível, ative-o em *Configurações › Personalização › Barra de tarefas › Outros ícones da bandeja do sistema*.

## Como funciona

Tudo usa APIs nativas do Windows, sem software de terceiros:

- **Listagem:** `EnumPrinters` (winspool.drv), nível 4, com impressoras locais e conexões de rede. O nível 4 só lê o registro, então não trava quando uma impressora de rede está desligada.
- **Fila:** `rundll32 printui.dll,PrintUIEntry /o /n "<impressora>"` abre a janela nativa "Ver o que está sendo impresso".
- **Configuração:** `%APPDATA%\BandejaImpressoras\config.json`.
- **Iniciar com o Windows:** valor `BandejaImpressoras` em `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

Todo o código está em [`src/BandejaImpressoras/Program.cs`](src/BandejaImpressoras/Program.cs).

## Compilar

Requer o [.NET SDK](https://dotnet.microsoft.com/download) (6 ou superior).

```powershell
.\build.ps1 -Versao 1.0.0
```

O resultado fica em `dist\BandejaImpressoras-1.0.0.zip`.

> Se a compilação falhar com "não foi possível copiar ... withSupportedRuntime.config", o caminho da pasta é longo demais (mais de 260 caracteres). Clone o repositório em um caminho mais curto, como `C:\src`.

## Publicar uma versão

O GitHub Actions compila a cada push. Ao enviar uma tag `v*`, ele também cria a Release com o `.zip`:

```powershell
git tag v1.0.1
git push origin v1.0.1
```
