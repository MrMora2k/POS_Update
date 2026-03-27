using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ApliqxPos.Services;

namespace ApliqxPos.Views;

/// <summary>
/// Theme customization dialog with full HSV color picker.
/// Each color row has a swatch + hex TextBox.
/// Clicking swatch opens a full color picker popup with hue slider + SV gradient.
/// </summary>
public partial class ThemeCustomizationWindow : Window
{
    private static readonly (string Key, string Label)[] ColorEntries =
    [
        ("AccentBrush", "لون التمييز الرئيسي"),
        ("WindowBackgroundBrush", "لون خلفية النافذة"),
        ("CardBackgroundBrush", "لون خلفية البطاقات"),
        ("SurfaceBrush", "لون السطح"),
        ("TextPrimaryBrush", "لون النص الرئيسي"),
        ("TextSecondaryBrush", "لون النص الثانوي"),
        ("TextMutedBrush", "لون النص الخافت"),
        ("SuccessBrush", "لون النجاح"),
        ("WarningBrush", "لون التحذير"),
        ("DangerBrush", "لون الخطر"),
        ("SidebarColor", "لون الشريط الجانبي")
    ];

    // Quick-access preset colors (expanded palette)
    private static readonly string[] PresetColors =
    [
        // Purples & Blues
        "#7C3AED", "#A855F7", "#6366F1", "#4F46E5", "#3B82F6",
        "#2563EB", "#1D4ED8", "#0EA5E9", "#06B6D4", "#0891B2",
        // Teals & Greens
        "#14B8A6", "#0D9488", "#10B981", "#059669", "#22C55E",
        "#16A34A", "#84CC16", "#65A30D",
        // Yellows, Oranges & Reds
        "#EAB308", "#F59E0B", "#D97706", "#F97316", "#EA580C",
        "#EF4444", "#DC2626", "#B91C1C",
        // Pinks
        "#EC4899", "#DB2777", "#BE185D", "#F43F5E",
        // Neutrals (light to dark)
        "#FFFFFF", "#F9FAFB", "#F3F4F6", "#E5E7EB", "#D1D5DB",
        "#9CA3AF", "#6B7280", "#4B5563", "#374151", "#1F2937",
        "#111827", "#0F0F23", "#1A1A2E", "#252547", "#1E1E3F"
    ];

    public ThemeCustomizationWindow()
    {
        InitializeComponent();
        BuildColorItems();
    }

    private void BuildColorItems()
    {
        ColorItemsPanel.Children.Clear();

        foreach (var (key, label) in ColorEntries)
        {
            var currentColor = ThemeService.Instance.GetCurrentColor(key);
            var currentHex = $"#{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}";

            // Row container
            var border = new Border
            {
                Background = (Brush)FindResource("SurfaceBrush"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            var textBlock = new TextBlock
            {
                Text = label,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(textBlock, 0);
            grid.Children.Add(textBlock);

            // Hex TextBox
            var hexBox = new TextBox
            {
                Text = currentHex,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.LeftToRight,
                MaxLength = 7,
                Tag = key
            };
            Grid.SetColumn(hexBox, 1);
            grid.Children.Add(hexBox);

            // Color swatch
            var swatch = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(currentColor),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(2),
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
                Tag = key
            };
            Grid.SetColumn(swatch, 2);
            grid.Children.Add(swatch);

            // Hex TextBox -> apply on Enter
            hexBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    ApplyHexInput((TextBox)s!, swatch);
                }
            };

            // Hex TextBox -> apply on lost focus
            hexBox.LostFocus += (s, e) => ApplyHexInput((TextBox)s!, swatch);

            // Swatch click -> show full color picker popup
            swatch.MouseLeftButtonUp += (s, e) =>
            {
                var clickedSwatch = (Border)s!;
                var resourceKey = (string)clickedSwatch.Tag!;
                ShowColorPickerPopup(clickedSwatch, resourceKey, hexBox);
            };

            border.Child = grid;
            ColorItemsPanel.Children.Add(border);
        }
    }

    private void ApplyHexInput(TextBox box, Border swatch)
    {
        var resourceKey = (string)box.Tag!;
        var hex = box.Text.Trim();
        if (!hex.StartsWith("#")) hex = "#" + hex;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            swatch.Background = new SolidColorBrush(color);
            ThemeService.Instance.SetCustomColor(resourceKey, color);
            box.Text = hex.ToUpper();
        }
        catch { }
    }

    /// <summary>
    /// Shows a full color picker popup with:
    /// 1. Quick-access preset color swatches (expanded to 50 colors)
    /// 2. Hue rainbow slider
    /// 3. Saturation/Value gradient picker
    /// 4. Live preview
    /// </summary>
    private void ShowColorPickerPopup(Border anchorSwatch, string resourceKey, TextBox hexBox)
    {
        var popup = new Popup
        {
            PlacementTarget = anchorSwatch,
            Placement = PlacementMode.Left,
            StaysOpen = false,
            AllowsTransparency = true
        };

        var popupBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 50)),
            CornerRadius = new CornerRadius(14),
            BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(14),
            Margin = new Thickness(8)
        };

        var mainStack = new StackPanel { Width = 280 };

        // ─── Preview + Hex ───
        var previewColor = ThemeService.Instance.GetCurrentColor(resourceKey);
        var previewBorder = new Border
        {
            Width = 252,
            Height = 40,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(previewColor),
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10)
        };
        mainStack.Children.Add(previewBorder);

        // ─── Saturation/Value Canvas (SV Picker) ───
        var svSize = 252;
        var svCanvas = new Canvas
        {
            Width = svSize,
            Height = 160,
            Margin = new Thickness(0, 0, 0, 10),
            Cursor = Cursors.Cross,
            ClipToBounds = true,
            FlowDirection = FlowDirection.LeftToRight
        };

        // SV gradient background (updated when hue changes)
        var svImage = new System.Windows.Controls.Image
        {
            Width = svSize,
            Height = 160,
            Stretch = Stretch.Fill
        };
        svCanvas.Children.Add(svImage);

        // SV cursor (crosshair indicator)
        var svCursor = new Ellipse
        {
            Width = 14,
            Height = 14,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(svCursor, svSize / 2.0 - 7);
        Canvas.SetTop(svCursor, 80 - 7);
        svCanvas.Children.Add(svCursor);

        mainStack.Children.Add(svCanvas);

        // ─── Hue Slider (Rainbow bar) ───
        var hueLabel = new TextBlock
        {
            Text = "تدرج اللون",
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainStack.Children.Add(hueLabel);

        var hueBorder = new Border
        {
            Height = 20,
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true,
            Margin = new Thickness(0, 0, 0, 12),
            FlowDirection = FlowDirection.LeftToRight
        };

        // Rainbow gradient for hue
        var hueGradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 0.0));
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 0.167));
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 0.333));
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.5));
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 0.667));
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 255), 0.833));
        hueGradient.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1.0));
        hueBorder.Background = hueGradient;

        var hueCanvas = new Canvas { Height = 20, Cursor = Cursors.Hand, Background = Brushes.Transparent };
        hueBorder.Child = hueCanvas;

        // Hue cursor
        var hueCursor = new Border
        {
            Width = 6,
            Height = 24,
            CornerRadius = new CornerRadius(3),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };
        Canvas.SetTop(hueCursor, -2);
        hueCanvas.Children.Add(hueCursor);

        mainStack.Children.Add(hueBorder);

        // ─── Quick Presets ───
        var presetLabel = new TextBlock
        {
            Text = "ألوان سريعة",
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 6)
        };
        mainStack.Children.Add(presetLabel);

        var wrap = new WrapPanel { Width = 252 };
        foreach (var presetHex in PresetColors)
        {
            var presetColor = (Color)ColorConverter.ConvertFromString(presetHex);
            var btn = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(presetColor),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                Cursor = Cursors.Hand,
                Tag = presetHex
            };

            btn.MouseLeftButtonDown += (s, e) =>
            {
                var hex = (string)((Border)s!).Tag!;
                var color = (Color)ColorConverter.ConvertFromString(hex);
                anchorSwatch.Background = new SolidColorBrush(color);
                hexBox.Text = hex.ToUpper();
                previewBorder.Background = new SolidColorBrush(color);
                ThemeService.Instance.SetCustomColor(resourceKey, color);
                popup.IsOpen = false;
            };

            btn.MouseEnter += (s, _) => ((Border)s!).BorderBrush = new SolidColorBrush(Colors.White);
            btn.MouseLeave += (s, _) => ((Border)s!).BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));

            wrap.Children.Add(btn);
        }
        mainStack.Children.Add(wrap);

        popupBorder.Child = mainStack;
        popup.Child = popupBorder;

        // ─── State ───
        double currentHue = 0;
        double currentSat = 1;
        double currentVal = 1;

        // Initialize from current color
        ColorToHSV(previewColor, out currentHue, out currentSat, out currentVal);

        // Update SV image for initial hue
        UpdateSVGradient(svImage, currentHue, svSize, 160);

        // Position cursors from current values
        Canvas.SetLeft(hueCursor, (currentHue / 360.0) * hueBorder.ActualWidth - 3);
        Canvas.SetLeft(svCursor, currentSat * svSize - 7);
        Canvas.SetTop(svCursor, (1 - currentVal) * 160 - 7);

        // ─── Event: Hue slider ───
        bool hueMouseDown = false;
        void UpdateHue(double x)
        {
            var w = hueBorder.ActualWidth > 0 ? hueBorder.ActualWidth : 252;
            x = Math.Max(0, Math.Min(x, w));
            currentHue = (x / w) * 360.0;
            Canvas.SetLeft(hueCursor, x - 3);
            UpdateSVGradient(svImage, currentHue, svSize, 160);
            ApplyCurrentColor(currentHue, currentSat, currentVal, previewBorder, anchorSwatch, hexBox, resourceKey);
        }

        hueCanvas.MouseLeftButtonDown += (s, e) => { hueMouseDown = true; hueCanvas.CaptureMouse(); UpdateHue(e.GetPosition(hueCanvas).X); };
        hueCanvas.MouseLeftButtonUp += (s, e) => { hueMouseDown = false; hueCanvas.ReleaseMouseCapture(); };
        hueCanvas.MouseMove += (s, e) => { if (hueMouseDown) UpdateHue(e.GetPosition(hueCanvas).X); };

        // ─── Event: SV Canvas ───
        bool svMouseDown = false;
        void UpdateSV(double x, double y)
        {
            x = Math.Max(0, Math.Min(x, svSize));
            y = Math.Max(0, Math.Min(y, 160));
            currentSat = x / svSize;
            currentVal = 1.0 - (y / 160.0);
            Canvas.SetLeft(svCursor, x - 7);
            Canvas.SetTop(svCursor, y - 7);
            ApplyCurrentColor(currentHue, currentSat, currentVal, previewBorder, anchorSwatch, hexBox, resourceKey);
        }

        svCanvas.MouseLeftButtonDown += (s, e) => { svMouseDown = true; svCanvas.CaptureMouse(); UpdateSV(e.GetPosition(svCanvas).X, e.GetPosition(svCanvas).Y); };
        svCanvas.MouseLeftButtonUp += (s, e) => { svMouseDown = false; svCanvas.ReleaseMouseCapture(); };
        svCanvas.MouseMove += (s, e) => { if (svMouseDown) UpdateSV(e.GetPosition(svCanvas).X, e.GetPosition(svCanvas).Y); };

        popup.IsOpen = true;

        // Defer cursor positioning after layout
        popup.Opened += (s, e) =>
        {
            var hueW = hueBorder.ActualWidth > 0 ? hueBorder.ActualWidth : 252;
            Canvas.SetLeft(hueCursor, (currentHue / 360.0) * hueW - 3);
        };
    }

    private void ApplyCurrentColor(double h, double s, double v, Border preview, Border swatch, TextBox hexBox, string resourceKey)
    {
        var color = HSVToColor(h, s, v);
        var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        preview.Background = new SolidColorBrush(color);
        swatch.Background = new SolidColorBrush(color);
        hexBox.Text = hex;
        ThemeService.Instance.SetCustomColor(resourceKey, color);
    }

    private void UpdateSVGradient(System.Windows.Controls.Image img, double hue, int width, int height)
    {
        var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            double val = 1.0 - (double)y / height;
            for (int x = 0; x < width; x++)
            {
                double sat = (double)x / width;
                var c = HSVToColor(hue, sat, val);
                int i = (y * width + x) * 4;
                pixels[i + 0] = c.B;
                pixels[i + 1] = c.G;
                pixels[i + 2] = c.R;
                pixels[i + 3] = 255;
            }
        }

        wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        img.Source = wb;
    }

    // ─── HSV Helpers ───
    private static Color HSVToColor(double h, double s, double v)
    {
        h = h % 360;
        if (h < 0) h += 360;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = v - c;
        double r, g, b;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    private static void ColorToHSV(Color color, out double h, out double s, out double v)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;

        if (delta == 0) h = 0;
        else if (max == r) h = 60 * (((g - b) / delta) % 6);
        else if (max == g) h = 60 * (((b - r) / delta) + 2);
        else h = 60 * (((r - g) / delta) + 4);

        if (h < 0) h += 360;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ResetColors_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "هل أنت متأكد من استعادة الألوان الافتراضية؟",
            "استعادة الافتراضي",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ThemeService.Instance.ResetCustomColors();
            BuildColorItems();
        }
    }
}
