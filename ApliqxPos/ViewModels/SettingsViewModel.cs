using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using ApliqxPos.Data;
using System.IO;
using System.Diagnostics;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for Settings screen.
/// Handles app configuration, language, theme, and database operations.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    // Language Settings
    [ObservableProperty]
    private string _selectedLanguage;

    [ObservableProperty]
    private bool _isArabic;

    [ObservableProperty]
    private bool _isEnglish;

    // Theme Settings
    [ObservableProperty]
    private bool _isDarkTheme = true;

    public bool IsLightTheme => ThemeService.Instance.ActivePreset == "Light";
    public bool IsTurquoiseTheme => ThemeService.Instance.ActivePreset == "Turquoise";

    [RelayCommand]
    private void ApplyThemePreset(string preset)
    {
        ThemeService.Instance.ApplyThemePreset(preset);
        IsDarkTheme = preset == "Dark";
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsTurquoiseTheme));
    }

    public bool IsRestaurantMode
    {
        get => ThemeService.Instance.IsRestaurantMode;
        set
        {
            if (ThemeService.Instance.IsRestaurantMode != value)
            {
                ThemeService.Instance.SetRestaurantMode(value);
                OnPropertyChanged();
            }
        }
    }

    // Business Info
    [ObservableProperty]
    private string _businessName = "ProPOS";

    [ObservableProperty]
    private string _businessPhone = string.Empty;

    [ObservableProperty]
    private string _businessAddress = string.Empty;

    // Currency Settings
    [ObservableProperty]
    private string _primaryCurrency = "IQD";

    [ObservableProperty]
    private decimal _exchangeRate = 1480;

    // Tax Settings
    [ObservableProperty]
    private bool _enableTax;

    [ObservableProperty]
    private decimal _taxRate = 15;

    // Inventory Settings
    [ObservableProperty]
    private int _lowStockThreshold = 5;

    // Database Info
    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _databaseSize = "0 KB";

    [ObservableProperty]
    private string _lastBackup = "لا يوجد";

    [ObservableProperty]
    private bool _isProcessing;

    // Print Settings
    [ObservableProperty]
    private string _printOutputType = "Printer"; // "Printer" or "PDF"

    [ObservableProperty]
    private string _selectedPrinter = string.Empty;

    [ObservableProperty]
    private string _pdfSavePath = string.Empty;

    [ObservableProperty]
    private string _receiptWidth = "80mm"; // "58mm", "80mm", "A4", "A5"

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _availablePaperSizes = new() { "58mm", "80mm", "A4", "A5" };

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<string> _availablePrinters = new();

    [ObservableProperty]
    private string _selectedKitchenPrinter = string.Empty;

    // Advanced Print Settings
    [ObservableProperty]
    private string _printHeaderCustomText = string.Empty;

    [ObservableProperty]
    private string _printFooterCustomText = "شكراً لزيارتكم";

    [ObservableProperty]
    private bool _printShowLogo;

    [ObservableProperty]
    private string _printLogoPath = string.Empty;

    [ObservableProperty]
    private bool _printShowCashierName = true;

    [ObservableProperty]
    private bool _printShowCustomerInfo = true;

    [ObservableProperty]
    private bool _printShowTaxDetails = false;

    [ObservableProperty]
    private bool _printShowDiscount = true;

    [ObservableProperty]
    private bool _isAdvancedSettingsOpen;

    [RelayCommand]
    private void OpenAdvancedSettings() => IsAdvancedSettingsOpen = true;

    [RelayCommand]
    private void CloseAdvancedSettings() => IsAdvancedSettingsOpen = false;

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsProcessing = true;
        await App.CheckForUpdatesAsync(
            owner: System.Windows.Application.Current.MainWindow,
            showNoUpdateMessage: true);
        IsProcessing = false;
    }

    public LocalizationService Localization => LocalizationService.Instance;

    /// <summary>
    /// Displays the current app version in Settings.
    /// </summary>
    public string AppVersion => $"الإصدار الحالي: {UpdateService.Instance.GetCurrentVersion().ToString(3)}";

    private readonly IUnitOfWork _unitOfWork;

    public SettingsViewModel()
    {
        var context = new AppDbContext();
        _unitOfWork = new UnitOfWork(context);

        // Initialize from current settings
        _selectedLanguage = Localization.CurrentLanguage == "ar" ? "ar" : "en";
        _isArabic = _selectedLanguage == "ar";
        _isEnglish = _selectedLanguage == "en";

        LoadPrinters();
        LoadDatabaseInfo();
        _ = LoadSettingsAsync();
    }

    // Font Settings
    public IEnumerable<string> FontFamilies => ThemeService.Instance.SystemFontFamilies;

    public string SelectedFontFamily
    {
        get => ThemeService.Instance.FontFamily;
        set
        {
            if (ThemeService.Instance.FontFamily != value)
            {
                ThemeService.Instance.SetFontFamily(value);
                OnPropertyChanged();
            }
        }
    }

    public double FontSizeHeader
    {
        get => ThemeService.Instance.FontSizeHeader;
        set
        {
            if (ThemeService.Instance.FontSizeHeader != value)
            {
                ThemeService.Instance.SetFontSizes(header: value);
                OnPropertyChanged();
            }
        }
    }

    public double FontSizeSubheader
    {
        get => ThemeService.Instance.FontSizeSubheader;
        set
        {
            if (ThemeService.Instance.FontSizeSubheader != value)
            {
                ThemeService.Instance.SetFontSizes(subHeader: value);
                OnPropertyChanged();
            }
        }
    }

    public double FontSizeButton
    {
        get => ThemeService.Instance.FontSizeButton;
        set
        {
            if (ThemeService.Instance.FontSizeButton != value)
            {
                ThemeService.Instance.SetFontSizes(button: value);
                OnPropertyChanged();
            }
        }
    }

    public double FontSizeCaption
    {
        get => ThemeService.Instance.FontSizeCaption;
        set
        {
            if (ThemeService.Instance.FontSizeCaption != value)
            {
                ThemeService.Instance.SetFontSizes(caption: value);
                OnPropertyChanged();
            }
        }
    }

    public double FontSizeBody
    {
        get => ThemeService.Instance.FontSize;
        set
        {
            if (ThemeService.Instance.FontSize != value)
            {
                ThemeService.Instance.SetFontSizes(standard: value);
                OnPropertyChanged();
            }
        }
    }

    [RelayCommand]
    private void ResetFonts()
    {
        ThemeService.Instance.SetFontSizes(header: 28, subHeader: 18, button: 14, caption: 12, standard: 14);
        SelectedFontFamily = "Segoe UI";
        OnPropertyChanged(nameof(FontSizeHeader));
        OnPropertyChanged(nameof(FontSizeSubheader));
        OnPropertyChanged(nameof(FontSizeButton));
        OnPropertyChanged(nameof(FontSizeCaption));
        OnPropertyChanged(nameof(FontSizeBody));
        OnPropertyChanged(nameof(SelectedFontFamily));
    }

    private void LoadPrinters()
    {
        AvailablePrinters.Clear();
        foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
        {
            AvailablePrinters.Add(printer);
        }

        // Set default if not set and printers available
        if (string.IsNullOrEmpty(SelectedPrinter) && AvailablePrinters.Count > 0)
        {
            SelectedPrinter = AvailablePrinters[0];
        }
    }

    [RelayCommand]
    private void BrowsePdfPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "اختر مجلد حفظ ملفات PDF",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            PdfSavePath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختر شعار المتجر",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            PrintLogoPath = dialog.FileName;
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _unitOfWork.Settings.GetAllSettingsAsync();

            if (settings.TryGetValue("BusinessName", out var name)) BusinessName = name;
            if (settings.TryGetValue("BusinessPhone", out var phone)) BusinessPhone = phone;
            if (settings.TryGetValue("BusinessAddress", out var addr)) BusinessAddress = addr;
            
            if (settings.TryGetValue("PrimaryCurrency", out var curr)) PrimaryCurrency = curr;
            if (settings.TryGetValue("ExchangeRate", out var rateStr) && decimal.TryParse(rateStr, out var rate)) ExchangeRate = rate;
            
            if (settings.TryGetValue("EnableTax", out var taxEnabledStr)) EnableTax = bool.Parse(taxEnabledStr);
            if (settings.TryGetValue("TaxRate", out var taxRateStr) && decimal.TryParse(taxRateStr, out var taxRate)) TaxRate = taxRate;

            // Print Settings
            if (settings.TryGetValue("PrintOutputType", out var outputType)) PrintOutputType = outputType;
            if (settings.TryGetValue("SelectedPrinter", out var printer)) SelectedPrinter = printer;
            if (settings.TryGetValue("SelectedKitchenPrinter", out var kitchenPrinter)) SelectedKitchenPrinter = kitchenPrinter;
            if (settings.TryGetValue("PdfSavePath", out var pdfPath)) PdfSavePath = pdfPath;
            if (settings.TryGetValue("ReceiptWidth", out var width)) ReceiptWidth = width;

            // Advanced Print Settings
            if (settings.TryGetValue("PrintHeaderCustomText", out var headerText)) PrintHeaderCustomText = headerText;
            if (settings.TryGetValue("PrintFooterCustomText", out var footerText)) PrintFooterCustomText = footerText;
            if (settings.TryGetValue("PrintLogoPath", out var logoPath)) PrintLogoPath = logoPath;
            if (settings.TryGetValue("PrintShowLogo", out var showLogo)) PrintShowLogo = bool.Parse(showLogo);
            if (settings.TryGetValue("PrintShowCashierName", out var showCashier)) PrintShowCashierName = bool.Parse(showCashier);
            if (settings.TryGetValue("PrintShowCustomerInfo", out var showCustomer)) PrintShowCustomerInfo = bool.Parse(showCustomer);
            if (settings.TryGetValue("PrintShowTaxDetails", out var showTax)) PrintShowTaxDetails = bool.Parse(showTax);
            if (settings.TryGetValue("PrintShowDiscount", out var showDiscount)) PrintShowDiscount = bool.Parse(showDiscount);
        }
        catch
        {
            // First run defaults
        }
    }

    private void LoadDatabaseInfo()
    {
        try
        {
            var dbPath = AppDbContext.GetDatabasePath();
            DatabasePath = dbPath;

            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                var sizeKb = fileInfo.Length / 1024.0;
                if (sizeKb >= 1024)
                {
                    DatabaseSize = $"{sizeKb / 1024:F2} MB";
                }
                else
                {
                    DatabaseSize = $"{sizeKb:F0} KB";
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }

    [RelayCommand]
    private void SetLanguage(string language)
    {
        SelectedLanguage = language;
        IsArabic = language == "ar";
        IsEnglish = language == "en";
        
        Localization.SetLanguage(language);
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeService.Instance.SetTheme(IsDarkTheme);
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        try
        {
            var sourcePath = AppDbContext.GetDatabasePath();
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupPath = Path.Combine(backupDir, $"apliqxpos_backup_{timestamp}.db");

            await Task.Run(() => File.Copy(sourcePath, backupPath, true));
            
            LastBackup = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        }
        catch
        {
            // Handle error - could show a dialog
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ResetDatabaseAsync()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        try
        {
            // First backup the current database
            await BackupDatabaseAsync();

            // Delete and recreate database
            var dbPath = AppDbContext.GetDatabasePath();
            
            if (File.Exists(dbPath))
            {
                await Task.Run(() => File.Delete(dbPath));
            }

            // Recreate database
            using var context = new AppDbContext();
            await context.Database.EnsureCreatedAsync();

            LoadDatabaseInfo();

            // Restart application
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                Process.Start(exePath);
                System.Windows.Application.Current.Shutdown();
            }
        }
        catch
        {
            // Handle error
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            Process.Start(new ProcessStartInfo
            {
                FileName = backupDir,
                UseShellExecute = true
            });
        }
        catch
        {
            // Handle error
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            IsProcessing = true;

            await _unitOfWork.Settings.SetValueAsync("BusinessName", BusinessName);
            await _unitOfWork.Settings.SetValueAsync("BusinessPhone", BusinessPhone);
            await _unitOfWork.Settings.SetValueAsync("BusinessAddress", BusinessAddress);
            
            await _unitOfWork.Settings.SetValueAsync("PrimaryCurrency", PrimaryCurrency);
            await _unitOfWork.Settings.SetValueAsync("ExchangeRate", ExchangeRate.ToString());
            
            await _unitOfWork.Settings.SetValueAsync("EnableTax", EnableTax.ToString());
            await _unitOfWork.Settings.SetValueAsync("TaxRate", TaxRate.ToString());

            // Print Settings
            await _unitOfWork.Settings.SetValueAsync("PrintOutputType", PrintOutputType);
            await _unitOfWork.Settings.SetValueAsync("SelectedPrinter", SelectedPrinter ?? "");
            await _unitOfWork.Settings.SetValueAsync("SelectedKitchenPrinter", SelectedKitchenPrinter ?? "");
            await _unitOfWork.Settings.SetValueAsync("PdfSavePath", PdfSavePath ?? "");
            await _unitOfWork.Settings.SetValueAsync("ReceiptWidth", ReceiptWidth);

            // Advanced Print Settings
            await _unitOfWork.Settings.SetValueAsync("PrintHeaderCustomText", PrintHeaderCustomText ?? "");
            await _unitOfWork.Settings.SetValueAsync("PrintFooterCustomText", PrintFooterCustomText ?? "");
            await _unitOfWork.Settings.SetValueAsync("PrintLogoPath", PrintLogoPath ?? "");
            await _unitOfWork.Settings.SetValueAsync("PrintShowLogo", PrintShowLogo.ToString());
            await _unitOfWork.Settings.SetValueAsync("PrintShowCashierName", PrintShowCashierName.ToString());
            await _unitOfWork.Settings.SetValueAsync("PrintShowCustomerInfo", PrintShowCustomerInfo.ToString());
            await _unitOfWork.Settings.SetValueAsync("PrintShowTaxDetails", PrintShowTaxDetails.ToString());
            await _unitOfWork.Settings.SetValueAsync("PrintShowDiscount", PrintShowDiscount.ToString());

            await _unitOfWork.SaveChangesAsync();
            
            // Notify other viewmodels via messenger if needed, or just relying on reload
            System.Windows.MessageBox.Show("تم حفظ الاعدادات بنجاح", "نجاح", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
             System.Windows.MessageBox.Show($"فشل الحفظ: {ex.Message}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
