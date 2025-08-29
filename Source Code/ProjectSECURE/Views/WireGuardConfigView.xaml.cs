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
        private string ipAddress = "85.246.65.217:9595";

        public WireGuardConfigView()
        {
            InitializeComponent();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Por favor, insira um nome.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{name}_info.conf",
                DefaultExt = ".conf",
                Filter = "Configuração WireGuard (*.conf)|*.conf|Todos os ficheiros (*.*)|*.*"
            };
            if (dlg.ShowDialog() != true)
            {
                // User cancelled
                return;
            }
            string outputFile = dlg.FileName;
            string url = $"http://{ipAddress}/download/{name}";

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "curl",
                    Arguments = $"\"{url}\" -o \"{outputFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show($"Download concluído: {outputFile}", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Falha ao fazer download.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Falha ao iniciar o processo curl.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
