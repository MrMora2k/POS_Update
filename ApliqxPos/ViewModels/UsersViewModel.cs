using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using ApliqxPos.Models;
using ApliqxPos.Services;
using MaterialDesignThemes.Wpf;

namespace ApliqxPos.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private bool _isLoading;

    public UsersViewModel()
    {
        LoadUsersCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadUsers()
    {
        IsLoading = true;
        try
        {
            var users = await AuthService.Instance.GetAllUsersAsync();
            Users = new ObservableCollection<User>(users);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading users: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddUser()
    {
        var vm = new UserDialogViewModel();
        var view = new Views.UserDialog { DataContext = vm };

        var result = await DialogHost.Show(view, "RootDialog");

        bool isConfirmed = result is bool b && b || 
                          (result is string s && bool.TryParse(s, out bool b2) && b2);

        if (isConfirmed)
        {
            try
            {
                await AuthService.Instance.CreateUserAsync(vm.Username, vm.Password, vm.SelectedRole);
                await LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task EditUser(User user)
    {
        if (user == null) return;

        var vm = new UserDialogViewModel(user);
        var view = new Views.UserDialog { DataContext = vm };

        var result = await DialogHost.Show(view, "RootDialog");

        bool isConfirmed = result is bool b && b || 
                          (result is string s && bool.TryParse(s, out bool b2) && b2);

        if (isConfirmed)
        {
            try
            {
                // Create a temporary user object with updated values
                var updatedUser = new User
                {
                    Id = user.Id,
                    Username = user.Username, // Username usually shouldn't change or needs check
                    Role = vm.SelectedRole,
                    PinCode = vm.PinCode
                };
                
                await AuthService.Instance.UpdateUserAsync(updatedUser, vm.Password);
                await LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task DeleteUser(User user)
    {
        if (user == null) return;

        var result = MessageBox.Show($"Are you sure you want to delete user '{user.Username}'?", 
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await AuthService.Instance.DeleteUserAsync(user.Id);
                await LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
