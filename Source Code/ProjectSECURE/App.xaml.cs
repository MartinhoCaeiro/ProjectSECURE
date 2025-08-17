using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProjectSECURE
{
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up: Delete any leftover temp DB file on exit (no merge needed)
            try
            {
                string tempDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ProjectSECURE_temp.db");
                if (System.IO.File.Exists(tempDbPath))
                    System.IO.File.Delete(tempDbPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App Exit] Temp DB delete error: {ex.Message}");
            }
            base.OnExit(e);
        }
        private System.Timers.Timer? dbSyncTimer;
        private const string ConfigFile = "userconfig.json";
        public string CurrentTheme { get; private set; } = "Light";

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
                // Inicia o timer para sincronização periódica
                dbSyncTimer = new System.Timers.Timer(20 * 1000); // 20 segundos
                dbSyncTimer.Elapsed += async (s, args) =>
                {
                    if (Views.LoginView.IsWireGuardInterfaceUp())
                    {
                        bool updated = await Services.DbSyncService.DownloadDatabaseAsync();
                        if (updated)
                        {
                            // Reload messages if in ChatView
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var win = Application.Current.MainWindow;
                                if (win?.GetType() == typeof(Views.ChatView) && win.DataContext is ProjectSECURE.ViewModels.ChatViewModel chatVM)
                                {
                                    System.Diagnostics.Debug.WriteLine("[DbSync] Reloading messages in ChatViewModel");
                                    var reloadMethod = chatVM.GetType().GetMethod("LoadMessages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    reloadMethod?.Invoke(chatVM, null);
                                }
                            });
                        }
                    }
                };
                dbSyncTimer.AutoReset = true;
                dbSyncTimer.Start();
            }
            else
            {
                MessageBox.Show(
                    "A VPN não está ativa. A base de dados local será usada.",
                    "VPN Desligada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // Inicializa a BD local depois do download
            Data.DatabaseService.InitializeDatabase();

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
