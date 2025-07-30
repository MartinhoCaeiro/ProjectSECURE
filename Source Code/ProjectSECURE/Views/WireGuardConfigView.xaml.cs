using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace ProjectSECURE.Views
{
    public partial class WireGuardConfigView : Window
    {
        private WireGuardConf _model = new();

        public WireGuardConfigView()
        {
            InitializeComponent();
            LoadConfigSafe();
        }

        private static string Expand(string path) =>
            Environment.ExpandEnvironmentVariables(path);

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Selecionar configuração WireGuard",
                Filter = "WireGuard conf (*.conf)|*.conf|Todos (*.*)|*.*",
                InitialDirectory = Environment.ExpandEnvironmentVariables(
                    @"%USERPROFILE%\AppData\Local\WireGuard\Configs")
            };

            if (dlg.ShowDialog() == true)
            {
                ConfigPathTextBox.Text = dlg.FileName;
                LoadConfigSafe();
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e) => LoadConfigSafe();

        private void LoadConfigSafe()
        {
            try
            {
                var path = Expand(ConfigPathTextBox.Text);

                if (!File.Exists(path))
                {
                    _model = new WireGuardConf(); // novo/limpo
                    PrivateKeyBox.Password = "";
                    AddressTextBox.Text = "";
                    DnsTextBox.Text = "";
                    ServerPublicKeyTextBox.Text = "";
                    AllowedIpsTextBox.Text = "";
                    EndpointTextBox.Text = "";
                    return;
                }

                _model = WireGuardConf.Load(path);

                PrivateKeyBox.Password = _model.Interface.PrivateKey ?? "";
                AddressTextBox.Text = _model.Interface.Address ?? "";
                DnsTextBox.Text = _model.Interface.DNS ?? "";
                ServerPublicKeyTextBox.Text = _model.Peer.PublicKey ?? "";
                AllowedIpsTextBox.Text = _model.Peer.AllowedIPs ?? "";
                EndpointTextBox.Text = _model.Peer.Endpoint ?? "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao ler o .conf: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Expand(ConfigPathTextBox.Text);
                var dir = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(dir))
                    throw new InvalidOperationException("Caminho inválido para o ficheiro .conf.");

                Directory.CreateDirectory(dir);

                // Atualiza o modelo
                _model.Interface.PrivateKey = PrivateKeyBox.Password.Trim();
                _model.Interface.Address = AddressTextBox.Text.Trim();
                _model.Interface.DNS = DnsTextBox.Text.Trim();

                _model.Peer.PublicKey = ServerPublicKeyTextBox.Text.Trim();
                _model.Peer.AllowedIPs = AllowedIpsTextBox.Text.Trim();
                _model.Peer.Endpoint = EndpointTextBox.Text.Trim();

                // Validações simples
                if (string.IsNullOrWhiteSpace(_model.Interface.PrivateKey))
                    throw new InvalidOperationException("PrivateKey (Interface) é obrigatório.");
                if (string.IsNullOrWhiteSpace(_model.Interface.Address))
                    throw new InvalidOperationException("Address (Interface) é obrigatório.");
                if (string.IsNullOrWhiteSpace(_model.Peer.PublicKey))
                    throw new InvalidOperationException("PublicKey (Peer) é obrigatório.");
                if (string.IsNullOrWhiteSpace(_model.Peer.AllowedIPs))
                    throw new InvalidOperationException("AllowedIPs (Peer) é obrigatório.");
                if (string.IsNullOrWhiteSpace(_model.Peer.Endpoint))
                    throw new InvalidOperationException("Endpoint (Peer) é obrigatório.");

                _model.Save(path);

                MessageBox.Show("Configuração guardada.", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao guardar o .conf: {ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }

    // ---------------- WireGuardConf (modelo + parser) ----------------

    internal sealed class WireGuardConf
    {
        public InterfaceSection Interface { get; } = new();
        public PeerSection Peer { get; } = new();

        private readonly List<string> _lines = new();

        public static WireGuardConf Load(string path)
        {
            var conf = new WireGuardConf();
            conf._lines.AddRange(File.ReadAllLines(path));

            string? section = null;

            foreach (var raw in conf._lines)
            {
                var line = raw.Trim();

                if (line.StartsWith("#") || line.StartsWith(";") || line.Length == 0)
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.ToLowerInvariant();
                    continue;
                }

                var idx = line.IndexOf('=');
                if (idx <= 0) continue;

                var key = line[..idx].Trim().ToLowerInvariant();
                var val = line[(idx + 1)..].Trim();

                switch (section)
                {
                    case "[interface]":
                        if (key == "privatekey") conf.Interface.PrivateKey = val;
                        else if (key == "address") conf.Interface.Address = val;
                        else if (key == "dns") conf.Interface.DNS = val;
                        break;

                    case "[peer]":
                        if (key == "publickey") conf.Peer.PublicKey = val;
                        else if (key == "allowedips") conf.Peer.AllowedIPs = val;
                        else if (key == "endpoint") conf.Peer.Endpoint = val;
                        break;
                }
            }

            return conf;
        }

        public void Save(string path)
        {
            if (_lines.Count == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("[Interface]");
                sb.AppendLine($"PrivateKey = {Interface.PrivateKey}");
                sb.AppendLine($"Address = {Interface.Address}");
                if (!string.IsNullOrWhiteSpace(Interface.DNS))
                    sb.AppendLine($"DNS = {Interface.DNS}");
                sb.AppendLine();
                sb.AppendLine("[Peer]");
                sb.AppendLine($"PublicKey = {Peer.PublicKey}");
                sb.AppendLine($"AllowedIPs = {Peer.AllowedIPs}");
                sb.AppendLine($"Endpoint = {Peer.Endpoint}");
                File.WriteAllText(path, sb.ToString());
                return;
            }

            var newLines = new List<string>(_lines);
            UpdateKey(newLines, "[Interface]", "PrivateKey", Interface.PrivateKey);
            UpdateKey(newLines, "[Interface]", "Address", Interface.Address);
            UpdateKey(newLines, "[Interface]", "DNS", Interface.DNS, removeIfEmpty: true);

            UpdateKey(newLines, "[Peer]", "PublicKey", Peer.PublicKey);
            UpdateKey(newLines, "[Peer]", "AllowedIPs", Peer.AllowedIPs);
            UpdateKey(newLines, "[Peer]", "Endpoint", Peer.Endpoint);

            File.WriteAllLines(path, newLines);
        }

        private static void UpdateKey(List<string> lines, string sectionName, string key, string? value, bool removeIfEmpty = false)
        {
            int secStart = FindSectionIndex(lines, sectionName);
            if (secStart < 0)
            {
                lines.Add("");
                lines.Add(sectionName);
                secStart = lines.Count - 1;
            }

            int nextSec = FindNextSectionIndex(lines, secStart + 1);
            int keyIdx = FindKeyIndex(lines, secStart + 1, nextSec, key);

            if (string.IsNullOrWhiteSpace(value))
            {
                if (removeIfEmpty && keyIdx >= 0)
                    lines.RemoveAt(keyIdx);
                return;
            }

            var newLine = $"{key} = {value}";

            if (keyIdx >= 0)
                lines[keyIdx] = newLine;
            else
            {
                int insertAt = nextSec >= 0 ? nextSec : lines.Count;
                lines.Insert(insertAt, newLine);
            }
        }

        private static int FindSectionIndex(List<string> lines, string sectionName)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i].Trim();
                if (t.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static int FindNextSectionIndex(List<string> lines, int start)
        {
            for (int i = start; i < lines.Count; i++)
            {
                var t = lines[i].Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                    return i;
            }
            return -1;
        }

        private static int FindKeyIndex(List<string> lines, int start, int end, string key)
        {
            if (end < 0) end = lines.Count;
            for (int i = start; i < end; i++)
            {
                var t = lines[i].Trim();
                if (t.StartsWith("#") || t.StartsWith(";") || t.Length == 0) continue;

                var idx = t.IndexOf('=');
                if (idx <= 0) continue;

                var k = t[..idx].Trim();
                if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public sealed class InterfaceSection
        {
            public string? PrivateKey { get; set; }
            public string? Address { get; set; }
            public string? DNS { get; set; }
        }

        public sealed class PeerSection
        {
            public string? PublicKey { get; set; }
            public string? AllowedIPs { get; set; }
            public string? Endpoint { get; set; }
        }
    }
}
