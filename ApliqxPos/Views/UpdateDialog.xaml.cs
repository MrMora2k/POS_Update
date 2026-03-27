using System.Windows;
using ApliqxPos.ViewModels;

namespace ApliqxPos.Views;

public partial class UpdateDialog : Window
{
    public UpdateDialog(string newVersion, string downloadUrl, string releaseNotes)
    {
        InitializeComponent();
        DataContext = new UpdateViewModel(newVersion, downloadUrl, releaseNotes);
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
