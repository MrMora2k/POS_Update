using System;
using System.Windows;
using System.Windows.Controls;

namespace ApliqxPos.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OpenAdvancedPrintSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new AdvancedPrintSettingsWindow
            {
                DataContext = this.DataContext // SettingsViewModel
            };
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح نافذة الإعدادات: {ex.Message}", "خطأ");
        }
    }
}
