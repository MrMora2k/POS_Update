using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Services;
using System.Windows;
using System.Threading.Tasks;

namespace ApliqxPos.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty; // For registration

    [ObservableProperty]
    private bool _isRegistrationMode;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel()
    {
        CheckRegistrationMode();
    }

    private async void CheckRegistrationMode()
    {
        IsBusy = true;
        bool hasOwner = await AuthService.Instance.HasOwnerAsync();
        IsRegistrationMode = !hasOwner;
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "يرجى إدخال اسم المستخدم وكلمة المرور";
            return;
        }

        IsBusy = true;
        StatusMessage = "جاري التحقق...";

        if (IsRegistrationMode)
        {
            if (Password != ConfirmPassword)
            {
                StatusMessage = "كلمة المرور غير متطابقة";
                IsBusy = false;
                return;
            }

            try
            {
                await AuthService.Instance.RegisterOwnerAsync(Username, Password);
                StatusMessage = "تم إنشاء حساب المالك بنجاح";
                await Task.Delay(1000);
                CompleteLogin();
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
            }
        }
        else
        {
            bool success = await AuthService.Instance.ProcessLoginAsync(Username, Password);
            if (success)
            {
                CompleteLogin();
            }
            else
            {
                StatusMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
            }
        }

        IsBusy = false;
    }

    private void CompleteLogin()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.DataContext == this)
            {
                window.DialogResult = true;
                window.Close();
                break;
            }
        }
    }
}
