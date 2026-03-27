using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;
using System.Windows;

namespace ApliqxPos.Services;

/// <summary>
/// Service for managing application localization (Arabic/English) with RTL/LTR support.
/// </summary>
public partial class LocalizationService : ObservableObject
{
    private const string ArabicCulture = "ar-IQ";
    private const string EnglishCulture = "en-US";

    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    public static LocalizationService Instance => _instance.Value;

    [ObservableProperty]
    private string _currentLanguage = "ar";

    [ObservableProperty]
    private FlowDirection _currentFlowDirection = FlowDirection.RightToLeft;

    [ObservableProperty]
    private string _currentCultureCode = ArabicCulture;

    public bool IsArabic => CurrentLanguage == "ar";
    public bool IsEnglish => CurrentLanguage == "en";

    private LocalizationService()
    {
        // Default to Arabic
        SetLanguage("ar");
    }

    /// <summary>
    /// Sets the application language and updates FlowDirection.
    /// </summary>
    /// <param name="languageCode">"ar" for Arabic, "en" for English</param>
    public void SetLanguage(string languageCode)
    {
        if (languageCode != "ar" && languageCode != "en")
            languageCode = "ar"; // Default to Arabic

        // 1. Load appropriate resource dictionary FIRST (Before notifying listeners)
        LoadLanguageResources(languageCode);

        // 2. Set Property (Triggers NotifyPropertyChanged -> MainViewModel updates)
        CurrentLanguage = languageCode;

        if (languageCode == "ar")
        {
            CurrentFlowDirection = FlowDirection.RightToLeft;
            CurrentCultureCode = ArabicCulture;
        }
        else
        {
            CurrentFlowDirection = FlowDirection.LeftToRight;
            CurrentCultureCode = EnglishCulture;
        }

        // Update thread culture
        var culture = new CultureInfo(CurrentCultureCode);

        // FORCE English/Western Numerals (123) for Arabic
        if (languageCode == "ar")
        {
            culture.NumberFormat.DigitSubstitution = DigitShapes.None;
            culture.NumberFormat.NativeDigits = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        }

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        OnPropertyChanged(nameof(IsArabic));
        OnPropertyChanged(nameof(IsEnglish));
    }

    /// <summary>
    /// Toggles between Arabic and English.
    /// </summary>
    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == "ar" ? "en" : "ar");
    }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    public string GetString(string key)
    {
        var app = Application.Current;
        if (app != null && app.Resources != null && app.Resources.Contains(key))
        {
            return app.Resources[key]?.ToString() ?? key;
        }
        return key;
    }

    /// <summary>
    /// Loads the appropriate language resource dictionary.
    /// </summary>
    private void LoadLanguageResources(string languageCode)
    {
        var app = Application.Current;
        if (app == null) return;

        // 1. Create and Add New Dictionary
        var resourcePath = languageCode == "ar"
            ? "Strings.ar.xaml"
            : "Strings.en.xaml";

        ResourceDictionary? newDict = null;
        try
        {
            newDict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/ApliqxPos;component/Resources/{resourcePath}", UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(newDict);
        }
        catch (Exception ex)
        {
             MessageBox.Show($"Failed to load new language: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             return; // Abort if we can't load the new language
        }

        // 2. Find and Remove Old Dictionaries (excluding the one we just added)
        var dictsToRemove = new List<ResourceDictionary>();
        foreach (var dict in app.Resources.MergedDictionaries)
        {
            // We verify it's a String dictionary and NOT the one we just added
            if (dict != newDict && dict.Source != null && dict.Source.OriginalString.Contains("Strings."))
            {
                dictsToRemove.Add(dict);
            }
        }

        foreach (var dict in dictsToRemove)
        {
            app.Resources.MergedDictionaries.Remove(dict);
        }
    }
}
