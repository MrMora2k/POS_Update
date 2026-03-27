using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Data;
using ApliqxPos.Models;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for Reports dashboard.
/// Displays sales statistics and business metrics.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;

    // Date Range
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _selectedPeriod = "Monthly";

    // Stats
    [ObservableProperty]
    private decimal _totalSales;

    [ObservableProperty]
    private decimal _totalProfit;

    [ObservableProperty]
    private int _totalTransactions;

    [ObservableProperty]
    private decimal _averageOrderValue;

    [ObservableProperty]
    private decimal _totalDebt;

    [ObservableProperty]
    private int _newCustomers;

    [ObservableProperty]
    private bool _isLoading;

    // Charts Data
    [ObservableProperty]
    private ISeries[] _salesTrendSeries = [];

    [ObservableProperty]
    private ISeries[] _categoryBreakdownSeries = [];

    [ObservableProperty]
    private ISeries[] _topProductsSeries = [];

    [ObservableProperty]
    private Axis[] _trendXAxes = [];

    [ObservableProperty]
    private Axis[] _topProductsYAxes = [];

    [ObservableProperty]
    private LabelVisual _title = new() { Text = "إحصائيات المبيعات", TextSize = 25, Paint = new SolidColorPaint(SKColors.Black) };

    [ObservableProperty]
    private ObservableCollection<Sale> _recentSales = new();

    public LocalizationService Localization => LocalizationService.Instance;

    public ReportsViewModel()
    {
        var context = new AppDbContext();
        _unitOfWork = new UnitOfWork(context);
        
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadStatsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadStatsAsync()
    {
        // Get sales data for the date range
        TotalSales = await _unitOfWork.Sales.GetTotalSalesAsync(StartDate, EndDate);
        TotalProfit = await _unitOfWork.Sales.GetTotalProfitAsync(StartDate, EndDate);

        // Get transaction count
        var sales = await _unitOfWork.Sales.GetByDateRangeAsync(StartDate, EndDate);
        TotalTransactions = sales.Count();
        
        if (TotalTransactions > 0)
        {
            AverageOrderValue = TotalSales / TotalTransactions;
        }
        else
        {
            AverageOrderValue = 0;
        }

        // Get total outstanding debt
        var customers = await _unitOfWork.Customers.GetCustomersWithDebtAsync();
        TotalDebt = customers.Sum(c => c.CurrentDebt);

        // New customers in date range
        var allCustomers = await _unitOfWork.Customers.GetAllAsync();
        NewCustomers = allCustomers.Count(c => c.CreatedAt >= StartDate && c.CreatedAt <= EndDate);

        // Load Chart Data
        await LoadChartDataAsync(sales);
        
        // Load Recent Sales
        RecentSales = new ObservableCollection<Sale>(sales.OrderByDescending(s => s.SaleDate).Take(10));
    }

    private async Task LoadChartDataAsync(IEnumerable<Sale> sales)
    {
        if (!sales.Any())
        {
            SalesTrendSeries = [];
            CategoryBreakdownSeries = [];
            TopProductsSeries = [];
            return;
        }

        // 1. Sales Trend (By Date)
        var trendData = sales
            .GroupBy(s => s.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) })
            .ToList();

        SalesTrendSeries = [
            new LineSeries<decimal>
            {
                Values = trendData.Select(d => d.Total).ToArray(),
                Name = "Sales",
                Fill = new SolidColorPaint(SKColors.BlueViolet.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.BlueViolet) { StrokeThickness = 3 },
                GeometrySize = 10
            }
        ];

        TrendXAxes = [
            new Axis
            {
                Labels = trendData.Select(d => d.Date.ToString("MM/dd")).ToArray(),
                LabelsRotation = 45,
                TextSize = 12
            }
        ];

        // 2. Category Breakdown
        var categoryData = sales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.Product?.Category?.Name ?? "Other")
            .OrderByDescending(g => g.Sum(i => i.Subtotal))
            .Take(5)
            .ToList();

        CategoryBreakdownSeries = categoryData.Select(g => 
            new PieSeries<decimal> 
            { 
                Values = [g.Sum(i => i.Subtotal)], 
                Name = g.Key,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:N0}"
            }).ToArray();

        // 3. Top Products
        var productData = sales
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductName)
            .OrderByDescending(g => g.Sum(i => i.Quantity))
            .Take(5)
            .Reverse()
            .ToList();

        TopProductsSeries = [
            new ColumnSeries<decimal>
            {
                Values = productData.Select(g => g.Sum(i => i.Quantity)).ToArray(),
                Name = "Qty Sold",
                Stroke = new SolidColorPaint(SKColors.Teal) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Teal.WithAlpha(180))
            }
        ];

        TopProductsYAxes = [
            new Axis
            {
                Labels = productData.Select(g => g.Key).ToArray(),
                TextSize = 12
            }
        ];
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetPeriodAsync(string period)
    {
        SelectedPeriod = period;
        
        switch (period)
        {
            case "Daily":
                StartDate = DateTime.Today;
                EndDate = DateTime.Today;
                break;
            case "Weekly":
                StartDate = DateTime.Today.AddDays(-7);
                EndDate = DateTime.Today;
                break;
            case "Monthly":
                StartDate = DateTime.Today.AddMonths(-1);
                EndDate = DateTime.Today;
                break;
            case "Yearly":
                StartDate = DateTime.Today.AddYears(-1);
                EndDate = DateTime.Today;
                break;
        }

        await LoadDataAsync();
    }
}
