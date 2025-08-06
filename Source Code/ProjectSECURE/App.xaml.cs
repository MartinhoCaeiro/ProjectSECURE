using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProjectSECURE
{
    public partial class App : Application
    {
        private const string ConfigFile = "userconfig.json";
        public string CurrentTheme { get; private set; } = "Light";

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Inicializa a BD local primeiro (sempre)
            Data.DatabaseService.InitializeDatabase();

            // Verifica se o adaptador WireGuard está ativo
            bool vpnAtiva = Views.LoginView.IsWireGuardInterfaceUp();

            if (vpnAtiva)
            {
                bool atualizado = await Services.DbSyncService.DownloadDatabaseAsync();

                if (!atualizado)
                {
                    MessageBox.Show(
                        "VPN ativa, mas não foi possível sincronizar com o servidor.\nA base de dados local será usada.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show(
                    "A VPN não está ativa. A base de dados local será usada.",
                    "VPN Desligada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // Carrega o tema antes de abrir qualquer janela
            string theme = LoadThemePreference() ?? (IsSystemInDarkMode() ? "Dark" : "Light");
            ApplyTheme(theme);

            // Abre a janela de login
            var login = new Views.LoginView();
            MainWindow = login;
            login.Show();
        }


        public void ApplyTheme(string theme)
        {
            Resources.MergedDictionaries.Clear();

            var dict = new ResourceDictionary
            {
                Source = new Uri($"Themes/{theme}Theme.xaml", UriKind.Relative)
            };

            Resources.MergedDictionaries.Add(dict);
            CurrentTheme = theme;
            SaveThemePreference(theme);
        }

        private string? LoadThemePreference()
        {
            if (!File.Exists(ConfigFile))
                return null;

            try
            {
                var json = File.ReadAllText(ConfigFile);
                var config = JsonSerializer.Deserialize<UserConfig>(json);
                return config?.UserThemePreference;
            }
            catch
            {
                return null;
            }
        }

        private void SaveThemePreference(string theme)
        {
            var config = new UserConfig { UserThemePreference = theme };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        private bool IsSystemInDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return (key?.GetValue("AppsUseLightTheme") is int value) && value == 0;
            }
            catch
            {
                return false;
            }
        }

        private class UserConfig
        {
            public string? UserThemePreference { get; set; }
        }
    }
}
