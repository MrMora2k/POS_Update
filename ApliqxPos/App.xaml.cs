using System.Windows;
using ApliqxPos.Services;
using ApliqxPos.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace ApliqxPos;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        // Early Exception Handling for static/constructor issues
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
             MessageBox.Show($"Fatal System Error: {(args.ExceptionObject as Exception)?.Message}\n\n{(args.ExceptionObject as Exception)?.StackTrace}", "Static/Constructor Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // UI Thread Exception Handling
        this.DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"UI Crash: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try 
        {
            // Prevent auto-shutdown when closing dialogs
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            base.OnStartup(e);
            
            // 0. Diagnostic Resource Loading
            LoadDiagnosticResources();

            // 1. Ensure Database is Created (Critical for Login)
            await AppDbContext.InitializeDatabaseAsync();

            // 2. Ensure Default Admin Exists (For rescue scenarios)
            await AuthService.Instance.EnsureDefaultAdminAsync();
            
            // 3. Initialize services
            InitializeServices();

            // 3. Activation Check
            if (!LicenseService.Instance.IsActivated())
            {
                var activationView = new Views.ActivationView();
                bool? result = activationView.ShowDialog();
                if (result != true)
                {
                    Shutdown();
                    return;
                }
            }

            // 4. Login Check
            var loginView = new Views.LoginView();
            bool? loginResult = loginView.ShowDialog();
            if (loginResult != true)
            {
                Shutdown();
                return;
            }

            // 5. Show Main Window
            var mainWindow = new MainWindow();
            
            // Re-enable auto shutdown
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            
            mainWindow.Show();

            // 6. Check for updates in background (silent - only shows dialog if update is available)
            _ = CheckForUpdatesAsync(mainWindow);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fatal Startup Error:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                "Application Failed to Start", MessageBoxButton.OK, MessageBoxImage.Stop);
            Shutdown();
        }
    }

    private void LoadDiagnosticResources()
    {
        var dictionaries = new List<string>
        {
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml",
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml",
            "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepPurple.xaml",
            "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Cyan.xaml",
            "pack://application:,,,/ApliqxPos;component/Resources/Converters.xaml",
            "pack://application:,,,/ApliqxPos;component/Resources/Strings.ar.xaml",
            "pack://application:,,,/ApliqxPos;component/Resources/Colors.xaml",
            "pack://application:,,,/ApliqxPos;component/Resources/Styles.xaml"
        };

        foreach (var uriString in dictionaries)
        {
            try
            {
                var dict = new ResourceDictionary { Source = new Uri(uriString, UriKind.Absolute) };
                Resources.MergedDictionaries.Add(dict);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"FAILED to load resource: {uriString}\n\nError: {ex.Message}", "Diagnostic Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void InitializeServices()
    {
        try 
        {
            // Initialize LocalizationService (defaults to Arabic)
            var localization = LocalizationService.Instance;
            localization.SetLanguage("ar");
            
            // Initialize ThemeService (Settings are loaded automatically in constructor)
            var theme = ThemeService.Instance;
            
            // Ensure resources are applied if this is the first run/no settings
            if (Application.Current.Resources["FontSize_Body"] == null)
            {
                 // Force update if resources missing (though constructor should handle this)
                 theme.SetTheme(theme.IsDarkMode);
            }
            
            // Configure LiveCharts to use Arabic-compatible font
            LiveCharts.Configure(config =>
                config.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('أ')));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Service Initialization Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Background update check. Shows dialog only if a newer version is found on GitHub.
    /// Called silently on startup. Can also be called manually ("Check for Updates" in Settings).
    /// </summary>
    public static async Task CheckForUpdatesAsync(Window? owner = null, bool showNoUpdateMessage = false)
    {
        try
        {
            var (hasUpdate, newVersion, downloadUrl, releaseNotes) = await UpdateService.Instance.CheckForUpdateAsync();
            
            if (hasUpdate && newVersion != null && downloadUrl != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new Views.UpdateDialog(newVersion, downloadUrl, releaseNotes ?? "لا توجد ملاحظات إصدار.");
                    dialog.Owner = owner ?? Application.Current.MainWindow;
                    dialog.ShowDialog();
                });
            }
            else if (showNoUpdateMessage)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("✅ البرنامج محدّث وأحدث إصدار مثبت.", "لا توجد تحديثات", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }
        catch
        {
            // Silently ignore if no internet or error
        }
    }
}
