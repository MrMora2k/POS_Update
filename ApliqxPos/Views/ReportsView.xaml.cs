using System.Windows.Controls;
using System.Windows.Data;
using LiveChartsCore.SkiaSharpView.WPF;

namespace ApliqxPos.Views;

/// <summary>
/// Interaction logic for ReportsView.xaml
/// </summary>
public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        
        // Force assembly load for XAML compiler resolution
        var _ = LiveChartsCore.LiveCharts.DefaultSettings;
        
        // Create charts programmatically to bypass MC1000 build error
        CreateCharts();
    }

    private void CreateCharts()
    {
        // Sales Trend Chart (Cartesian - Line)
        var salesTrendChart = new CartesianChart();
        salesTrendChart.SetBinding(CartesianChart.SeriesProperty, new Binding("SalesTrendSeries"));
        salesTrendChart.SetBinding(CartesianChart.XAxesProperty, new Binding("TrendXAxes"));
        SalesTrendChartHost.Content = salesTrendChart;

        // Top Products Chart (Cartesian - Column)
        var topProductsChart = new CartesianChart { Height = 320 };
        topProductsChart.SetBinding(CartesianChart.SeriesProperty, new Binding("TopProductsSeries"));
        topProductsChart.SetBinding(CartesianChart.XAxesProperty, new Binding("TopProductsYAxes"));
        TopProductsChartHost.Content = topProductsChart;

        // Category Breakdown Chart (Pie)
        var categoryBreakdownChart = new PieChart { Height = 320 };
        categoryBreakdownChart.SetBinding(PieChart.SeriesProperty, new Binding("CategoryBreakdownSeries"));
        CategoryBreakdownChartHost.Content = categoryBreakdownChart;
    }
}
