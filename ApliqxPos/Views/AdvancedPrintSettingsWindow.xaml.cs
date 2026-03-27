using System.Windows;

namespace ApliqxPos.Views;

/// <summary>
/// Interaction logic for AdvancedPrintSettingsWindow.xaml
/// </summary>
public partial class AdvancedPrintSettingsWindow : Window
{
    public AdvancedPrintSettingsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
