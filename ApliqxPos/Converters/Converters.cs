using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ApliqxPos.Converters;

/// <summary>
/// Converts a value to Visibility.Visible if it equals the parameter, else Collapsed.
/// </summary>
public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;
        
        return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if value equals parameter, false otherwise. For use with RadioButton IsChecked binding.
/// </summary>
public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;
        
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Return the parameter when checked (true)
        if (value is bool isChecked && isChecked)
            return parameter;
        
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts a value to Visibility.Visible if it does NOT equal the parameter, else Collapsed.
/// </summary>
public class NotEqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Visible;
        
        return value.ToString() != parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean value to Visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// Inverts a boolean value and converts to Visibility.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to uppercase.
/// </summary>
public class UpperCaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str.ToUpperInvariant();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a decimal value to formatted currency string (IQD by default).
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            var currencyCode = parameter?.ToString() ?? "IQD";
            return Helpers.CurrencyHelper.Format(amount, currencyCode);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && Helpers.CurrencyHelper.TryParse(str, out var amount))
        {
            return amount;
        }
        return 0m;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return value;
    }
}

/// <summary>
/// Converts null to Visibility.Collapsed, non-null to Visible.
/// Pass "Inverse" as parameter to invert (null = Visible, non-null = Collapsed).
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNullOrEmpty = value == null || (value is string str && string.IsNullOrWhiteSpace(str));
        bool inverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (inverse)
            return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
        
        return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts stock level to background color brush.
/// </summary>
public class StockToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && decimal.TryParse(value.ToString(), out decimal stock))
        {
            if (stock <= 0)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 239, 68, 68)); // Red
            if (stock <= 5)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 245, 158, 11)); // Warning
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 16, 185, 129)); // Success
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to text based on parameter (format: TrueText|FalseText).
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && parameter is string param)
        {
            var parts = param.Split('|');
            if (parts.Length == 2)
            {
                return boolValue ? parts[0] : parts[1];
            }
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts debt amount to background color brush.
/// </summary>
public class DebtToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal debt)
        {
            if (debt <= 0)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 16, 185, 129)); // Green for no debt
            if (debt > 0)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 239, 68, 68)); // Red for debt
        }
        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts debt amount to Visibility (Visible if debt > 0).
/// </summary>
public class DebtToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal debt && debt > 0)
            return Visibility.Visible;
        
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts stock level to status text.
/// </summary>
public class StockToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value != null && decimal.TryParse(value.ToString(), out decimal stock))
        {
            if (stock <= 0)
                return "نفاد"; // Out of stock
            if (stock <= 5)
                return "منخفض"; // Low
            return "متوفر"; // Available
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts value to bool based on equality with parameter.
/// </summary>
public class EqualityToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;
        
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
            return parameter;
        
        return Binding.DoNothing;
    }
}
