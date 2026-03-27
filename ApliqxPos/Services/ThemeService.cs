using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;

namespace ApliqxPos.Services;

/// <summary>
/// Service for managing application themes (Dark/Light) and typography settings.
/// </summary>
public partial class ThemeService : ObservableObject
{
    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;

    private readonly PaletteHelper _paletteHelper = new();

    [ObservableProperty]
    private bool _isDarkMode = true;

    [ObservableProperty]
    private string _fontFamily = "Segoe UI";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private FontWeight _fontWeight = FontWeights.Normal;
    [ObservableProperty]
    private double _fontSizeHeader = 28;

    [ObservableProperty]
    private double _fontSizeSubheader = 18;

    [ObservableProperty]
    private double _fontSizeButton = 14;

    [ObservableProperty]
    private double _fontSizeCaption = 12;

    [ObservableProperty]
    private bool _isRestaurantMode = false;

    public IEnumerable<string> SystemFontFamilies => Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x);

    // Persistence path
    private readonly string _settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    private ThemeService()
    {
        LoadSettings();
    }

    /// <summary>
    /// Sets the business mode (Restaurant vs Store).
    /// </summary>
    public void SetRestaurantMode(bool isRestaurant)
    {
        IsRestaurantMode = isRestaurant;
        SaveSettings();
    }

    /// <summary>
    /// Sets the application theme (Dark or Light).
    /// </summary>
    public void SetTheme(bool isDark)
    {
        IsDarkMode = isDark;

        var theme = _paletteHelper.GetTheme();
        if (isDark)
            theme.SetBaseTheme(new MaterialDesignDarkTheme());
        else
            theme.SetBaseTheme(new MaterialDesignLightTheme());

        _paletteHelper.SetTheme(theme);
        UpdateThemeResources();
        SaveSettings();
    }

    /// <summary>
    /// Toggles between Dark and Light theme.
    /// </summary>
    public void ToggleTheme()
    {
        SetTheme(!IsDarkMode);
    }

    /// <summary>
    /// Sets just the font family.
    /// </summary>
    public void SetFontFamily(string fontFamily)
    {
        FontFamily = fontFamily;
        UpdateTypographyResources();
        SaveSettings();
    }

    /// <summary>
    /// Sets specific font component sizes.
    /// </summary>
    public void SetFontSizes(double? header = null, double? subHeader = null, double? button = null, double? caption = null, double? standard = null)
    {
        if (header.HasValue) FontSizeHeader = header.Value;
        if (subHeader.HasValue) FontSizeSubheader = subHeader.Value;
        if (button.HasValue) FontSizeButton = button.Value;
        if (caption.HasValue) FontSizeCaption = caption.Value;
        if (standard.HasValue) FontSize = standard.Value;

        UpdateTypographyResources();
        SaveSettings();
    }

    private void UpdateThemeResources()
    {
        var app = Application.Current;
        if (app == null) return;

        // Update background and foreground based on theme
        if (IsDarkMode)
        {
            app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Colors.White);
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
        }
        else
        {
            app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Colors.White);
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
        }
    }

    private void UpdateTypographyResources()
    {
        var app = Application.Current;
        if (app == null) return;

        app.Resources["AppFontFamily"] = new FontFamily(FontFamily);
        app.Resources["AppFontSize"] = FontSize; // Body
        app.Resources["AppFontWeight"] = FontWeight;

        // New granular keys
        app.Resources["FontSize_H1"] = FontSizeHeader;
        app.Resources["FontSize_H2"] = FontSizeSubheader;
        app.Resources["FontSize_Button"] = FontSizeButton;
        app.Resources["FontSize_Caption"] = FontSizeCaption;
        app.Resources["FontSize_Body"] = FontSize;
    }

    private void LoadSettings()
    {
        try
        {
            if (System.IO.File.Exists(_settingsPath))
            {
                var json = System.IO.File.ReadAllText(_settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    IsDarkMode = settings.IsDarkMode;
                    IsRestaurantMode = settings.IsRestaurantMode;
                    FontFamily = settings.FontFamily ?? "Segoe UI";
                    FontSize = settings.FontSize > 0 ? settings.FontSize : 14;
                    FontSizeHeader = settings.FontSizeHeader > 0 ? settings.FontSizeHeader : 28;
                    FontSizeSubheader = settings.FontSizeSubheader > 0 ? settings.FontSizeSubheader : 18;
                    FontSizeButton = settings.FontSizeButton > 0 ? settings.FontSizeButton : 14;
                    FontSizeCaption = settings.FontSizeCaption > 0 ? settings.FontSizeCaption : 12;

                    // Apply loaded settings
                    SetTheme(IsDarkMode); // Applies theme
                    UpdateTypographyResources(); // Applies typo
                    return;
                }
            }
        }
        catch { /* Ignore load errors, fallback to defaults */ }

        // Defaults if no file or error
        SetTheme(true);
        UpdateTypographyResources();
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new AppSettings
            {
                IsDarkMode = IsDarkMode,
                IsRestaurantMode = IsRestaurantMode,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontSizeHeader = FontSizeHeader,
                FontSizeSubheader = FontSizeSubheader,
                FontSizeButton = FontSizeButton,
                FontSizeCaption = FontSizeCaption
            };
            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            System.IO.File.WriteAllText(_settingsPath, json);
        }
        catch { /* Ignore save errors */ }
    }

    // Settings DTO
    private class AppSettings
    {
        public bool IsDarkMode { get; set; }
        public bool IsRestaurantMode { get; set; }
        public string? FontFamily { get; set; }
        public double FontSize { get; set; }
        public double FontSizeHeader { get; set; }
        public double FontSizeSubheader { get; set; }
        public double FontSizeButton { get; set; }
        public double FontSizeCaption { get; set; }
    }
}
