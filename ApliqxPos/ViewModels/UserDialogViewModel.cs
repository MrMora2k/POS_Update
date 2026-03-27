using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ApliqxPos.Models;

namespace ApliqxPos.ViewModels;

public partial class UserDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _pinCode = string.Empty;

    [ObservableProperty]
    private UserRole _selectedRole = UserRole.Cashier;

    public ObservableCollection<UserRole> Roles { get; } = new(Enum.GetValues<UserRole>());
    
    public bool IsEditMode { get; }

    public UserDialogViewModel()
    {
        IsEditMode = false;
    }

    public UserDialogViewModel(User existingUser)
    {
        IsEditMode = true;
        Username = existingUser.Username;
        SelectedRole = existingUser.Role;
        PinCode = existingUser.PinCode ?? string.Empty;
        // Password not pre-filled for security
    }
}
