using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace ApliqxKeyGen;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        base.OnStartup(e);

        try
        {
            LoadDiagnosticResources();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fatal Startup Error: {ex.Message}", "KeyGen Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        string errorMessage = $"An unhandled exception occurred: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";
        File.WriteAllText("error.log", errorMessage);
        MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void LoadDiagnosticResources()
    {
        var dictionaries = new List<string>
        {
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml",
            "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml",
            "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepPurple.xaml",
            "pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Lime.xaml"
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
                MessageBox.Show($"FAILED to load resource: {uriString}\n\nError: {ex.Message}", "Startup Resource Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
