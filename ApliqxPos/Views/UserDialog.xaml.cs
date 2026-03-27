using System.Windows.Controls;

namespace ApliqxPos.Views;

public partial class UserDialog : UserControl
{
    public UserDialog()
    {
        InitializeComponent();
        
        // Ensure the password box updates the view model (since PasswordBox doesn't bind automatically)
        var passwordBox = this.FindName("PasswordBox") as PasswordBox;
        if (passwordBox != null)
        {
            passwordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is ViewModels.UserDialogViewModel vm)
                {
                    vm.Password = passwordBox.Password;
                }
            };
        }
    }
}
