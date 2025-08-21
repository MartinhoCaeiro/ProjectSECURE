using Microsoft.Win32;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ProjectSECURE
{
    public partial class App : Application
    {
        // Timer for periodic database synchronization
        private System.Timers.Timer? dbSyncTimer;
        private const string ConfigFile = "userconfig.json";
        public string CurrentTheme { get; private set; } = "Light";

        // Clean up temporary database file on exit
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                string tempDbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ProjectSECURE_temp.db");
                if (System.IO.File.Exists(tempDbPath))
                    System.IO.File.Delete(tempDbPath);
            }
            catch
            {
                // Ignore errors during cleanup
            }
            base.OnExit(e);
        }

        // Application startup logic
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check if WireGuard VPN adapter is active
            bool vpnActive = Views.LoginView.IsWireGuardInterfaceUp();

            if (vpnActive)
            {
                // Try to download the latest database from the server
                bool updated = await Services.DbSyncService.DownloadDatabaseAsync();

                if (!updated)
                {
                    MessageBox.Show(
                        "VPN está ativa, mas não foi possível sincronizar com o servidor. A base de dados local será usada.",
                        "Aviso!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                // Start periodic database sync every 20 seconds
                dbSyncTimer = new System.Timers.Timer(20 * 1000);
                dbSyncTimer.Elapsed += async (s, args) =>
                {
                    if (Views.LoginView.IsWireGuardInterfaceUp())
                    {
                        bool dbUpdated = await Services.DbSyncService.DownloadDatabaseAsync();
                        if (dbUpdated)
                        {
                            // If ChatView is open, reload messages after sync
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var win = Application.Current.MainWindow;
                                if (win?.GetType() == typeof(Views.ChatView) && win.DataContext is ProjectSECURE.ViewModels.ChatViewModel chatVM)
                                {
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
                    "VPN não está ativa. A base de dados local será usada.",
                    "Aviso!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            // Initialize local database after download
            Data.DatabaseService.InitializeDatabase();

            // Load theme before opening any window
            string theme = LoadThemePreference() ?? (IsSystemInDarkMode() ? "Dark" : "Light");
            ApplyTheme(theme);

            // Show login window
            var login = new Views.LoginView();
            MainWindow = login;
            login.Show();
        }

        // Apply the selected theme to the application
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

        // Load theme preference from config file
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

        // Save theme preference to config file
        private void SaveThemePreference(string theme)
        {
            var config = new UserConfig { UserThemePreference = theme };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        // Detect if Windows system is in dark mode
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

        // Model for user configuration file
        private class UserConfig
        {
            public string? UserThemePreference { get; set; }
        }
    }
}
