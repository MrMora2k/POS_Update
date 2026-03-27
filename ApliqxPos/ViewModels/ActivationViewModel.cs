using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Services;
using System.Windows;
using System.Threading.Tasks;

namespace ApliqxPos.ViewModels;

public partial class ActivationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _licenseKey = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ActivationViewModel()
    {
    }

    [RelayCommand]
    private async Task Activate()
    {
        if (string.IsNullOrWhiteSpace(LicenseKey) || 
            string.IsNullOrWhiteSpace(Username) || 
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "يرجى إدخال جميع البيانات المطلوبة";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var (success, message, ownerUser, ownerPassHash) = await LicenseService.Instance.ActivateLicenseAsync(LicenseKey, Username, Password);

            if (success)
            {
                // Sync local owner credentials with the activated license
                await AuthService.Instance.SyncOwnerCredentialsAsync(Username, Password);
                
                // Restart Application to proceed to Login
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var mainModule = currentProcess.MainModule;
                if (mainModule != null)
                {
                    System.Diagnostics.Process.Start(mainModule.FileName);
                }
                Application.Current.Shutdown();
            }
            else
            {
                ErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseWindow(Window? window)
    {
        window?.Close();
    }

    [RelayCommand]
    private void Skip(Window? window)
    {
        if (window != null)
        {
            window.DialogResult = true;
            window.Close();
        }
    }
}
