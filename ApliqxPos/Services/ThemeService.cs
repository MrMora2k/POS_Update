using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;
using System.Text.Json;
using System.IO;

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
    private string _activePreset = "Dark";

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

    // Custom theme colors
    private Dictionary<string, string> _customColors = new();

    // Default color map for reset
    private static readonly Dictionary<string, string> DefaultColors = new()
    {
        ["AccentBrush"] = "#7C3AED",
        ["WindowBackgroundBrush"] = "#0F0F23",
        ["CardBackgroundBrush"] = "#252547",
        ["SurfaceBrush"] = "#1E1E3F",
        ["TextPrimaryBrush"] = "#FFFFFF",
        ["TextSecondaryBrush"] = "#A0A0C0",
        ["TextMutedBrush"] = "#6B6B8D",
        ["SuccessBrush"] = "#10B981",
        ["WarningBrush"] = "#F59E0B",
        ["DangerBrush"] = "#EF4444",
        ["SidebarColor"] = "#1A1A2E"
    };

    // Preset: Light
    private static readonly Dictionary<string, string> LightPreset = new()
    {
        ["AccentBrush"] = "#7C3AED",
        ["WindowBackgroundBrush"] = "#F5F5F5",
        ["CardBackgroundBrush"] = "#FFFFFF",
        ["SurfaceBrush"] = "#EEEEEE",
        ["TextPrimaryBrush"] = "#212121",
        ["TextSecondaryBrush"] = "#757575",
        ["TextMutedBrush"] = "#9E9E9E",
        ["SuccessBrush"] = "#10B981",
        ["WarningBrush"] = "#F59E0B",
        ["DangerBrush"] = "#EF4444",
        ["SidebarColor"] = "#E0E0E0"
    };

    // Preset: Turquoise / Off-White
    private static readonly Dictionary<string, string> TurquoisePreset = new()
    {
        ["AccentBrush"] = "#0D9488",
        ["WindowBackgroundBrush"] = "#FAF9F6",
        ["CardBackgroundBrush"] = "#F5F5F0",
        ["SurfaceBrush"] = "#EFEDE8",
        ["TextPrimaryBrush"] = "#1A1A1A",
        ["TextSecondaryBrush"] = "#5F6368",
        ["TextMutedBrush"] = "#9AA0A6",
        ["SuccessBrush"] = "#059669",
        ["WarningBrush"] = "#D97706",
        ["DangerBrush"] = "#DC2626",
        ["SidebarColor"] = "#0F766E"
    };

    public Dictionary<string, string> CustomColors => _customColors;

    public IEnumerable<string> SystemFontFamilies => Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(x => x);

    // Persistence path
    private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

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
    /// Applies a named theme preset (Dark, Light, Turquoise).
    /// </summary>
    public void ApplyThemePreset(string presetName)
    {
        ActivePreset = presetName;

        var presetColors = presetName switch
        {
            "Light" => LightPreset,
            "Turquoise" => TurquoisePreset,
            _ => DefaultColors
        };

        // Update MaterialDesign base theme
        var isDark = presetName == "Dark";
        IsDarkMode = isDark;
        var theme = _paletteHelper.GetTheme();
        if (isDark)
            theme.SetBaseTheme(new MaterialDesignDarkTheme());
        else
            theme.SetBaseTheme(new MaterialDesignLightTheme());

        // Sync MaterialDesign primary color with preset accent
        try
        {
            var accentColor = (Color)ColorConverter.ConvertFromString(presetColors["AccentBrush"]);
            theme.SetPrimaryColor(accentColor);
            theme.SetSecondaryColor(accentColor);
        }
        catch { }

        _paletteHelper.SetTheme(theme);

        // Apply all preset colors
        _customColors = new Dictionary<string, string>(presetColors);

        var app = Application.Current;
        if (app == null) return;

        foreach (var kvp in presetColors)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(kvp.Value);
                if (kvp.Key == "SidebarColor")
                {
                    var gradient = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1)
                    };
                    gradient.GradientStops.Add(new GradientStop(color, 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb(
                        (byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7)), 1));
                    app.Resources["SidebarGradient"] = gradient;
                }
                else
                {
                    app.Resources[kvp.Key] = new SolidColorBrush(color);
                }
            }
            catch { }
        }

        // Also update PrimaryGradient for accent color
        try
        {
            var accentColor = (Color)ColorConverter.ConvertFromString(presetColors["AccentBrush"]);
            var accentGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            accentGradient.GradientStops.Add(new GradientStop(accentColor, 0));
            accentGradient.GradientStops.Add(new GradientStop(Color.FromRgb(
                (byte)Math.Min(255, accentColor.R + 40),
                (byte)Math.Min(255, accentColor.G + 40),
                (byte)Math.Min(255, accentColor.B + 40)), 1));
            app.Resources["PrimaryGradient"] = accentGradient;
        }
        catch { }

        // Update BackgroundGradient from window background color
        try
        {
            var bgColor = (Color)ColorConverter.ConvertFromString(presetColors["WindowBackgroundBrush"]);
            var bgGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            bgGradient.GradientStops.Add(new GradientStop(bgColor, 0));
            bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(
                (byte)Math.Min(255, bgColor.R + 10),
                (byte)Math.Min(255, bgColor.G + 10),
                (byte)Math.Min(255, bgColor.B + 10)), 0.5));
            bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(
                (byte)Math.Min(255, bgColor.R + 20),
                (byte)Math.Min(255, bgColor.G + 20),
                (byte)Math.Min(255, bgColor.B + 20)), 1));
            app.Resources["BackgroundGradient"] = bgGradient;
            app.Resources["BackgroundBrush"] = new SolidColorBrush(bgColor);
        }
        catch { }

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

    /// <summary>
    /// Sets a single custom color and applies it live.
    /// </summary>
    public void SetCustomColor(string resourceKey, Color color)
    {
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        _customColors[resourceKey] = hex;

        var app = Application.Current;
        if (app == null) return;

        if (resourceKey == "SidebarColor")
        {
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            gradient.GradientStops.Add(new GradientStop(color, 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb((byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7)), 1));
            app.Resources["SidebarGradient"] = gradient;
        }
        else
        {
            app.Resources[resourceKey] = new SolidColorBrush(color);
        }

        SaveSettings();
    }

    /// <summary>
    /// Gets the current color for a resource key.
    /// </summary>
    public Color GetCurrentColor(string resourceKey)
    {
        if (_customColors.TryGetValue(resourceKey, out var hex))
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); } catch { }
        }
        if (DefaultColors.TryGetValue(resourceKey, out var defHex))
        {
            try { return (Color)ColorConverter.ConvertFromString(defHex); } catch { }
        }
        return Colors.White;
    }

    /// <summary>
    /// Applies all custom colors from the dictionary to Application.Resources.
    /// </summary>
    public void ApplyCustomColors()
    {
        var app = Application.Current;
        if (app == null) return;

        foreach (var kvp in _customColors)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(kvp.Value);
                if (kvp.Key == "SidebarColor")
                {
                    var gradient = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1)
                    };
                    gradient.GradientStops.Add(new GradientStop(color, 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb((byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7)), 1));
                    app.Resources["SidebarGradient"] = gradient;
                }
                else
                {
                    app.Resources[kvp.Key] = new SolidColorBrush(color);
                }
            }
            catch { }
        }

        // Also rebuild BackgroundGradient if WindowBackgroundBrush was customized
        if (_customColors.TryGetValue("WindowBackgroundBrush", out var bgHex))
        {
            try
            {
                var bgColor = (Color)ColorConverter.ConvertFromString(bgHex);
                var bgGradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                bgGradient.GradientStops.Add(new GradientStop(bgColor, 0));
                bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(
                    (byte)Math.Min(255, bgColor.R + 10),
                    (byte)Math.Min(255, bgColor.G + 10),
                    (byte)Math.Min(255, bgColor.B + 10)), 0.5));
                bgGradient.GradientStops.Add(new GradientStop(Color.FromRgb(
                    (byte)Math.Min(255, bgColor.R + 20),
                    (byte)Math.Min(255, bgColor.G + 20),
                    (byte)Math.Min(255, bgColor.B + 20)), 1));
                app.Resources["BackgroundGradient"] = bgGradient;
                app.Resources["BackgroundBrush"] = new SolidColorBrush(bgColor);
            }
            catch { }
        }
    }

    /// <summary>
    /// Resets all custom colors to defaults and re-applies them.
    /// </summary>
    public void ResetCustomColors()
    {
        _customColors.Clear();

        var app = Application.Current;
        if (app == null) return;

        foreach (var kvp in DefaultColors)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(kvp.Value);
                if (kvp.Key == "SidebarColor")
                {
                    var gradient = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(0, 1)
                    };
                    gradient.GradientStops.Add(new GradientStop(color, 0));
                    gradient.GradientStops.Add(new GradientStop(Color.FromRgb((byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7)), 1));
                    app.Resources["SidebarGradient"] = gradient;
                }
                else
                {
                    app.Resources[kvp.Key] = new SolidColorBrush(color);
                }
            }
            catch { }
        }

        SaveSettings();
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
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
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

                    // Load custom colors
                    if (settings.CustomColors != null)
                        _customColors = new Dictionary<string, string>(settings.CustomColors);

                    // Load active preset
                    ActivePreset = settings.ActivePreset ?? "Dark";

                    // Save user's custom overrides before preset overwrites _customColors
                    var userOverrides = settings.CustomColors != null 
                        ? new Dictionary<string, string>(settings.CustomColors) 
                        : null;

                    // Apply loaded settings using the preset system
                    UpdateTypographyResources();
                    ApplyThemePreset(ActivePreset);

                    // Re-apply user custom overrides on top of preset
                    if (userOverrides != null && userOverrides.Count > 0)
                    {
                        _customColors = new Dictionary<string, string>(userOverrides);
                        ApplyCustomColors();
                    }
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
                FontSizeCaption = FontSizeCaption,
                CustomColors = _customColors.Count > 0 ? new Dictionary<string, string>(_customColors) : null,
                ActivePreset = ActivePreset
            };
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(_settingsPath, json);
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
        public Dictionary<string, string>? CustomColors { get; set; }
        public string? ActivePreset { get; set; }
    }
}
