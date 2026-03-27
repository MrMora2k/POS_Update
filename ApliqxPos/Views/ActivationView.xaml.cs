using System.Windows;

namespace ApliqxPos.Views;

public partial class ActivationView : Window
{
    public ActivationView()
    {
        InitializeComponent();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UserPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ActivationViewModel viewModel)
        {
            viewModel.Password = ((System.Windows.Controls.PasswordBox)sender).Password;
        }
    }
}
