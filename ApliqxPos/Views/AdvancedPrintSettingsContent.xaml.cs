using System.Windows;
using System.Windows.Controls;

namespace ApliqxPos.Views;

/// <summary>
/// Interaction logic for AdvancedPrintSettingsContent.xaml
/// </summary>
public partial class AdvancedPrintSettingsContent : UserControl
{
    public AdvancedPrintSettingsContent()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the window hosting this user control
        var window = Window.GetWindow(this);
        window?.Close();
    }
}
