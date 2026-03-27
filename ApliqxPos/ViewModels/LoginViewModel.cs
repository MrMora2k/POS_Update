using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Services;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Threading.Tasks;

namespace ApliqxPos.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    // Saved credentials path (stored locally, never on server)
    private static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProPOS", "savedcreds.json");

    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private bool _isRegistrationMode;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _rememberMe;

    public LoginViewModel()
    {
        LoadSavedCredentials();
        CheckRegistrationMode();
    }

    private void LoadSavedCredentials()
    {
        try
        {
            if (File.Exists(CredentialsPath))
            {
                var json = File.ReadAllText(CredentialsPath);
                var creds = JsonSerializer.Deserialize<SavedCredentials>(json);
                if (creds != null && creds.Remember)
                {
                    Username = creds.Username ?? string.Empty;
                    Password = creds.Password ?? string.Empty;
                    RememberMe = true;
                }
            }
        }
        catch { /* ignore */ }
    }

    private void SaveOrClearCredentials()
    {
        try
        {
            var dir = Path.GetDirectoryName(CredentialsPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (RememberMe)
            {
                var creds = new SavedCredentials { Username = Username, Password = Password, Remember = true };
                File.WriteAllText(CredentialsPath, JsonSerializer.Serialize(creds));
            }
            else
            {
                if (File.Exists(CredentialsPath)) File.Delete(CredentialsPath);
            }
        }
        catch { /* ignore */ }
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
                StatusMessage = "✅ تم إنشاء حساب المالك بنجاح";
                SaveOrClearCredentials();
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
                SaveOrClearCredentials();
                CompleteLogin();
            }
            else
            {
                StatusMessage = "❌ اسم المستخدم أو كلمة المرور غير صحيحة";
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

    private record SavedCredentials
    {
        public string? Username { get; init; }
        public string? Password { get; init; }
        public bool Remember { get; init; }
    }
}
