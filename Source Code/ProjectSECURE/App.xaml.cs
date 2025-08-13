using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProjectSECURE
{
    public partial class App : Application
    {
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
                            // Salva o tipo da janela atual
                            Type? currentWindowType = null;
                            object? user = null;
                            object? chat = null;
                            bool isWireGuardActive = false;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var win = Application.Current.MainWindow;
                                currentWindowType = win?.GetType();
                                if (win != null)
                                {
                                    var dc = win.DataContext;
                                    if (dc != null)
                                    {
                                        var userProp = dc.GetType().GetProperty("CurrentUser") ?? dc.GetType().GetProperty("User");
                                        if (userProp != null) user = userProp.GetValue(dc);
                                        var chatProp = dc.GetType().GetProperty("CurrentChat") ?? dc.GetType().GetProperty("Chat");
                                        if (chatProp != null) chat = chatProp.GetValue(dc);
                                        var wgProp = dc.GetType().GetProperty("IsWireGuardActive");
                                        if (wgProp != null && wgProp.PropertyType == typeof(bool) && wgProp.GetValue(dc) != null) isWireGuardActive = (bool)wgProp.GetValue(dc);
                                    }
                                    if (win.GetType().Name == "NewChatView")
                                    {
                                        var field = win.GetType().GetField("currentUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (field != null) user = field.GetValue(win);
                                    }
                                }
                                // If in ChatView, just reload messages
                                if (currentWindowType == typeof(Views.ChatView) && win?.DataContext is ProjectSECURE.ViewModels.ChatViewModel chatVM)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        System.Diagnostics.Debug.WriteLine("[DbSync] Reloading messages in ChatViewModel");
                                        var reloadMethod = chatVM.GetType().GetMethod("LoadMessages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        reloadMethod?.Invoke(chatVM, null);
                                    });
                                }
                                // For other windows, do nothing (UI updates only when entering)
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
