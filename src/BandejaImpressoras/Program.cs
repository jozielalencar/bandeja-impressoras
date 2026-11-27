// Bandeja de Impressoras — ícone na bandeja do sistema para abrir a fila de impressão.
// Clique esquerdo: abre a fila de impressão da impressora padrão.
// Clique direito: trocar a impressora padrão, iniciar com o Windows, sair.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BandejaImpressoras
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Apenas uma instância por usuário.
            using (var mutex = new Mutex(true, @"Local\BandejaImpressoras", out bool primeiraInstancia))
            {
                if (!primeiraInstancia) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Qualquer erro inesperado vira um aviso simples, sem derrubar o programa.
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) =>
                    MessageBox.Show("Ocorreu um erro:\n" + e.Exception.Message, "Bandeja de Impressoras",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                Application.Run(new TrayApp());
            }
        }
    }

    class TrayApp : ApplicationContext
    {
        readonly NotifyIcon icone;
        readonly ContextMenuStrip menuDireito = new ContextMenuStrip();
        readonly Config config = Config.Carregar();
        bool dialogoAberto;
        DateTime ultimaAbertura;

        public TrayApp()
        {
            icone = new NotifyIcon
            {
                Icon = CarregarIcone(),
                ContextMenuStrip = menuDireito,
                Visible = true,
            };
            icone.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) AbrirFilaPadrao(); };
            menuDireito.Opening += (s, e) => { MontarMenuDireito(); e.Cancel = false; };
            AtualizarDica();

            // Verifica a impressora padrão logo depois que o loop de mensagens começa.
            var timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s, e) => { timer.Dispose(); VerificarPadrao(); };
            timer.Start();
        }

        // ---------- Clique esquerdo ----------

        void AbrirFilaPadrao()
        {
            // Um duplo clique gera dois cliques: abre a fila uma vez só.
            if ((DateTime.Now - ultimaAbertura).TotalSeconds < 1) return;
            ultimaAbertura = DateTime.Now;

            var impressoras = Spooler.ListarImpressoras();
            if (impressoras.Count == 0)
            {
                icone.ShowBalloonTip(5000, "Bandeja de Impressoras", "Nenhuma impressora instalada.", ToolTipIcon.Warning);
                return;
            }

            // Sem padrão válida: pede uma e só então abre a fila.
            if (!Contem(impressoras, config.ImpressoraPadrao))
            {
                EscolherPadrao(impressoras, MensagemEscolha());
                if (!Contem(impressoras, config.ImpressoraPadrao)) return;
            }
            AbrirFila(config.ImpressoraPadrao);
        }

        // ---------- Clique direito ----------

        void MontarMenuDireito()
        {
            var impressoras = Spooler.ListarImpressoras();
            menuDireito.Items.Clear();

            string atual = config.ImpressoraPadrao ?? "(nenhuma)";
            menuDireito.Items.Add(new ToolStripMenuItem("Padrão: " + atual) { Enabled = false });

            var abrir = new ToolStripMenuItem("Abrir fila da impressora padrão")
            {
                Enabled = config.ImpressoraPadrao != null && Contem(impressoras, config.ImpressoraPadrao),
            };
            abrir.Click += (s, e) => AbrirFila(config.ImpressoraPadrao);
            menuDireito.Items.Add(abrir);

            var trocar = new ToolStripMenuItem("Trocar impressora padrão");
            if (impressoras.Count == 0)
                trocar.DropDownItems.Add(new ToolStripMenuItem("Nenhuma impressora instalada") { Enabled = false });
            foreach (var nome in impressoras)
            {
                var item = ItemImpressora(nome);
                item.Click += (s, e) => DefinirPadrao(nome);
                trocar.DropDownItems.Add(item);
            }
            menuDireito.Items.Add(trocar);

            menuDireito.Items.Add(new ToolStripSeparator());

            var iniciar = new ToolStripMenuItem("Iniciar com o Windows") { Checked = Autostart.Ativo() };
            iniciar.Click += (s, e) =>
            {
                try { Autostart.Definir(!iniciar.Checked); }
                catch (Exception ex)
                {
                    icone.ShowBalloonTip(8000, "Iniciar com o Windows",
                        "Não foi possível alterar a inicialização automática: " + ex.Message +
                        " Um antivírus pode estar bloqueando a alteração.", ToolTipIcon.Warning);
                }
            };
            menuDireito.Items.Add(iniciar);

            menuDireito.Items.Add(new ToolStripSeparator());

            var sair = new ToolStripMenuItem("Sair");
            sair.Click += (s, e) => { icone.Visible = false; Application.Exit(); };
            menuDireito.Items.Add(sair);
        }

        ToolStripMenuItem ItemImpressora(string nome)
        {
            bool padrao = string.Equals(nome, config.ImpressoraPadrao, StringComparison.OrdinalIgnoreCase);
            var item = new ToolStripMenuItem(nome.Replace("&", "&&")) { Checked = padrao };
            if (padrao) item.Font = new Font(item.Font, FontStyle.Bold);
            return item;
        }

        // ---------- Ações ----------

        void VerificarPadrao()
        {
            var impressoras = Spooler.ListarImpressoras();
            if (impressoras.Count == 0)
            {
                icone.ShowBalloonTip(5000, "Bandeja de Impressoras", "Nenhuma impressora instalada.", ToolTipIcon.Warning);
                return;
            }

            if (!Contem(impressoras, config.ImpressoraPadrao))
                EscolherPadrao(impressoras, MensagemEscolha());
        }

        string MensagemEscolha() => config.ImpressoraPadrao == null
            ? "Escolha a impressora padrão:"
            : $"A impressora \"{config.ImpressoraPadrao}\" não foi encontrada.\nEscolha outra impressora padrão:";

        void EscolherPadrao(List<string> impressoras, string mensagem)
        {
            if (dialogoAberto) return;
            dialogoAberto = true;
            try
            {
                using (var dlg = new DialogoEscolha(impressoras, config.ImpressoraPadrao, mensagem))
                {
                    if (dlg.ShowDialog() == DialogResult.OK && dlg.Escolhida != null)
                        DefinirPadrao(dlg.Escolhida);
                }
            }
            finally { dialogoAberto = false; }
        }

        void DefinirPadrao(string nome)
        {
            config.ImpressoraPadrao = nome;
            config.Salvar();
            AtualizarDica();
        }

        void AbrirFila(string nome)
        {
            if (!Contem(Spooler.ListarImpressoras(), nome))
            {
                icone.ShowBalloonTip(5000, "Bandeja de Impressoras", $"A impressora \"{nome}\" não foi encontrada.", ToolTipIcon.Warning);
                return;
            }
            try
            {
                // Janela nativa "Ver o que está sendo impresso" (fila de trabalhos) da impressora.
                Process.Start("rundll32.exe", $"printui.dll,PrintUIEntry /o /n \"{nome}\"");
            }
            catch (Exception ex)
            {
                icone.ShowBalloonTip(5000, "Erro ao abrir a fila", ex.Message, ToolTipIcon.Error);
            }
        }

        void AtualizarDica()
        {
            string dica = "Impressora padrão: " + (config.ImpressoraPadrao ?? "(nenhuma)");
            icone.Text = dica.Length > 63 ? dica.Substring(0, 60) + "..." : dica; // limite do Windows
        }

        static bool Contem(List<string> impressoras, string nome) =>
            nome != null && impressoras.Any(i => string.Equals(i, nome, StringComparison.OrdinalIgnoreCase));

        static Icon CarregarIcone()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
                return s != null ? new Icon(s, SystemInformation.SmallIconSize) : SystemIcons.Application;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { icone.Dispose(); menuDireito.Dispose(); }
            base.Dispose(disposing);
        }
    }

    /// <summary>Janela pequena de seleção (primeira execução / impressora removida).</summary>
    class DialogoEscolha : Form
    {
        readonly ListBox lista = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        public string Escolhida => lista.SelectedItem as string;

        public DialogoEscolha(List<string> impressoras, string atual, string mensagem)
        {
            Text = "Bandeja de Impressoras";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.Font;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(380, 300);
            Padding = new Padding(10);

            var rotulo = new Label { Text = mensagem, Dock = DockStyle.Top, AutoSize = false, Height = 40 };
            lista.Items.AddRange(impressoras.ToArray());
            int i = atual == null ? -1 : impressoras.FindIndex(p => string.Equals(p, atual, StringComparison.OrdinalIgnoreCase));
            lista.SelectedIndex = i >= 0 ? i : 0;
            lista.DoubleClick += (s, e) => { if (Escolhida != null) DialogResult = DialogResult.OK; };

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90 };
            var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 };
            var botoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft,
                Height = 42, Padding = new Padding(0, 8, 0, 0),
            };
            botoes.Controls.Add(cancelar);
            botoes.Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancelar;

            Controls.Add(lista);
            Controls.Add(botoes);
            Controls.Add(rotulo);
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); Activate(); lista.Focus(); }
    }

    /// <summary>Lista de impressoras via API do spooler (winspool.drv / EnumPrinters).</summary>
    static class Spooler
    {
        const int PRINTER_ENUM_LOCAL = 0x2;
        const int PRINTER_ENUM_CONNECTIONS = 0x4;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct PRINTER_INFO_4
        {
            public string pPrinterName;
            public string pServerName;
            public int Attributes;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool EnumPrinters(int flags, string name, int level, IntPtr buffer, int cbBuf,
                                        out int pcbNeeded, out int pcReturned);

        // Nível 4 só lê o registro (não consulta servidores de rede), por isso é rápido e não trava
        // quando uma impressora compartilhada está desligada.
        public static List<string> ListarImpressoras()
        {
            var nomes = new List<string>();
            const int flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;
            try
            {
                EnumPrinters(flags, null, 4, IntPtr.Zero, 0, out int necessario, out _);
                if (necessario <= 0) return nomes;

                IntPtr buffer = Marshal.AllocHGlobal(necessario);
                try
                {
                    if (!EnumPrinters(flags, null, 4, buffer, necessario, out necessario, out int total))
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    int tamanho = Marshal.SizeOf(typeof(PRINTER_INFO_4));
                    for (int i = 0; i < total; i++)
                    {
                        var info = (PRINTER_INFO_4)Marshal.PtrToStructure(IntPtr.Add(buffer, i * tamanho), typeof(PRINTER_INFO_4));
                        if (!string.IsNullOrEmpty(info.pPrinterName)) nomes.Add(info.pPrinterName);
                    }
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            catch (Exception ex)
            {
                // Spooler parado, por exemplo: devolve lista vazia em vez de derrubar o programa.
                Debug.WriteLine(ex);
            }
            return nomes.Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
        }
    }

    /// <summary>Configuração em %APPDATA%\BandejaImpressoras\config.json.</summary>
    [DataContract]
    class Config
    {
        static readonly string Arquivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BandejaImpressoras", "config.json");

        [DataMember(Name = "impressoraPadrao")]
        public string ImpressoraPadrao { get; set; }

        public static Config Carregar()
        {
            try
            {
                if (File.Exists(Arquivo))
                    using (var s = File.OpenRead(Arquivo))
                        return (Config)new DataContractJsonSerializer(typeof(Config)).ReadObject(s) ?? new Config();
            }
            catch (Exception ex) { Debug.WriteLine(ex); } // arquivo corrompido: começa do zero
            return new Config();
        }

        public void Salvar()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Arquivo));
                using (var s = File.Create(Arquivo))
                    new DataContractJsonSerializer(typeof(Config)).WriteObject(s, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível salvar a configuração:\n" + ex.Message, "Bandeja de Impressoras",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>Entrada em HKCU\Software\Microsoft\Windows\CurrentVersion\Run.</summary>
    static class Autostart
    {
        const string Chave = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string Valor = "BandejaImpressoras";
        static string Comando => "\"" + Application.ExecutablePath + "\"";

        public static bool Ativo()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(Chave))
                return string.Equals(k?.GetValue(Valor) as string, Comando, StringComparison.OrdinalIgnoreCase);
        }

        public static void Definir(bool ativo)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(Chave))
            {
                if (ativo) k.SetValue(Valor, Comando);
                else k.DeleteValue(Valor, false);
            }
        }
    }
}
