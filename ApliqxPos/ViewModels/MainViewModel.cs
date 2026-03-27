using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Services;
using ApliqxPos.Models;
using System.Windows;

namespace ApliqxPos.ViewModels;

/// <summary>
/// Main ViewModel for the application shell.
/// Handles navigation, theme switching, and language switching.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentView = "Dashboard";

    [ObservableProperty]
    private string _pageTitle = "لوحة التحكم";

    [ObservableProperty]
    private bool _isNavigationDrawerOpen = true;

    // Dashboard Statistics
    [ObservableProperty]
    private string _totalSales = "0";

    [ObservableProperty]
    private string _totalProfit = "0";

    [ObservableProperty]
    private int _totalCustomers = 0;

    [ObservableProperty]
    private string _totalDebts = "0";

    public LocalizationService Localization => LocalizationService.Instance;
    public ThemeService Theme => ThemeService.Instance;

    // Navigation menu items
    public List<NavigationItem> NavigationItems { get; }

    [ObservableProperty]
    private UsersViewModel? _usersViewModel;

    public User? CurrentUser => AuthService.Instance.CurrentUser;

    public MainViewModel()
    {
        NavigationItems =
        [
            new NavigationItem("Dashboard", "ViewDashboard", "Nav_Dashboard", UserRole.Cashier),
            new NavigationItem("POS", "PointOfSale", "Nav_POS", UserRole.Cashier),
            new NavigationItem("Products", "Package", "Nav_Products", UserRole.Admin),
            new NavigationItem("Categories", "FolderMultiple", "Nav_Categories", UserRole.Admin),
            new NavigationItem("Inventory", "Warehouse", "Nav_Inventory", UserRole.Admin),
            new NavigationItem("Customers", "AccountGroup", "Nav_Customers", UserRole.Cashier),
            new NavigationItem("Debts", "CreditCard", "Nav_Debts", UserRole.Admin),
            new NavigationItem("Sales", "CartCheck", "Nav_Sales", UserRole.Admin),
            new NavigationItem("Reports", "ChartBar", "Nav_Reports", UserRole.Owner),
            new NavigationItem("Users", "AccountMultiple", "Nav_Users", UserRole.Owner) { Title = "المستخدمين" },
            new NavigationItem("Settings", "Cog", "Nav_Settings", UserRole.Owner)
        ];

        // START: Localization title updates
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(() =>
        {
            foreach (var item in NavigationItems)
            {
                if (item.TitleKey == "Nav_Users")
                    item.Title = "المستخدمين"; // Fallback/Default for Arabic
                else
                    item.UpdateTitle();
            }
        });

        UpdatePageTitle();
        
        Localization.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LocalizationService.CurrentLanguage))
            {
                UpdatePageTitle();
                foreach (var item in NavigationItems)
                {
                    if (item.TitleKey == "Nav_Users")
                         item.Title = "المستخدمين"; // Hardcoded for now
                    else
                         item.UpdateTitle();
                }
                OnPropertyChanged(nameof(Localization));
            }
        };
        
        // Listen to Auth changes
        AuthService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AuthService.CurrentUser))
            {
                OnPropertyChanged(nameof(CurrentUser));
                UpdateNavigationVisibility();
            }
        };
        
        UpdateNavigationVisibility();
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        if (viewName == "Users")
        {
            UsersViewModel ??= new UsersViewModel();
        }
        CurrentView = viewName;
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        Localization.ToggleLanguage();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        Theme.ToggleTheme();
    }

    [RelayCommand]
    private void ToggleNavigationDrawer()
    {
        IsNavigationDrawerOpen = !IsNavigationDrawerOpen;
    }

    [RelayCommand]
    private void Logout()
    {
        AuthService.Instance.Logout();
        
        // Restart Application to return to Login
        System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        Application.Current.Shutdown();
    }

    partial void OnCurrentViewChanged(string value)
    {
        UpdatePageTitle();
        UpdateNavigationSelection();
    }

    private void UpdatePageTitle()
    {
        var navItem = NavigationItems.FirstOrDefault(n => n.ViewName == CurrentView);
        if (navItem != null)
        {
            if (navItem.ViewName == "Users")
                 PageTitle = "المستخدمين";
            else
                 PageTitle = Localization.GetString(navItem.TitleKey);
        }
    }

    private void UpdateNavigationSelection()
    {
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.ViewName == CurrentView;
        }
    }
    
    private void UpdateNavigationVisibility()
    {
        if (CurrentUser == null) return;

        foreach (var item in NavigationItems)
        {
            bool isVisible = false;

            // Define hierarchy: Owner > Admin > Cashier
            switch (CurrentUser.Role)
            {
                case UserRole.Owner:
                    isVisible = true; // Owner sees everything
                    break;
                case UserRole.Admin:
                    // Admin sees everything EXCEPT Owner-only items
                    isVisible = item.RequiredRole != UserRole.Owner;
                    break;
                case UserRole.Cashier:
                    // Cashier sees only Cashier items
                    isVisible = item.RequiredRole == UserRole.Cashier;
                    break;
            }

            item.IsVisible = isVisible;
        }

        // If current view becomes hidden, redirect to Dashboard or POS
        var currentItem = NavigationItems.FirstOrDefault(n => n.ViewName == CurrentView);
        if (currentItem != null && !currentItem.IsVisible)
        {
            CurrentView = "POS";
        }
    }
}

/// <summary>
/// Represents a navigation menu item.
/// </summary>
public partial class NavigationItem : ObservableObject
{
    public string ViewName { get; }
    public string IconKind { get; }
    public string TitleKey { get; }
    public UserRole RequiredRole { get; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    public NavigationItem(string viewName, string iconKind, string titleKey, UserRole requiredRole = UserRole.Cashier)
    {
        ViewName = viewName;
        IconKind = iconKind;
        TitleKey = titleKey;
        RequiredRole = requiredRole;
    }

    public void UpdateTitle()
    {
        if (string.IsNullOrEmpty(TitleKey)) return;
        Title = LocalizationService.Instance.GetString(TitleKey);
    }
}
