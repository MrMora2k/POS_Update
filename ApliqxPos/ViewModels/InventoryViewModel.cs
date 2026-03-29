using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Models;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using ApliqxPos.Data;
using System.Collections.ObjectModel;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for Inventory management screen.
/// Handles stock tracking, low stock alerts, and stock adjustments.
/// </summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showAdjustStockDialog;

    [ObservableProperty]
    private bool _showLowStockOnly;

    [ObservableProperty]
    private bool _showOutOfStockOnly;

    // Stock adjustment fields
    [ObservableProperty]
    private decimal _adjustmentQuantity;

    [ObservableProperty]
    private string _adjustmentType = "Add"; // Add or Remove

    [ObservableProperty]
    private string _adjustmentNotes = string.Empty;

    // Stats
    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _outOfStockCount;

    [ObservableProperty]
    private decimal _totalInventoryValue;

    public LocalizationService Localization => LocalizationService.Instance;

    public InventoryViewModel()
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
            IEnumerable<Product> products;

            if (ShowOutOfStockOnly)
            {
                products = await _unitOfWork.Products.GetOutOfStockAsync();
            }
            else if (ShowLowStockOnly)
            {
                products = await _unitOfWork.Products.GetLowStockAsync();
            }
            else if (SelectedCategory != null && SelectedCategory.Id != 0)
            {
                products = await _unitOfWork.Products.GetByCategoryAsync(SelectedCategory.Id);
            }
            else
            {
                products = await _unitOfWork.Products.GetAllAsync();
            }

            Products = new ObservableCollection<Product>(products.Where(p => p.IsActive));

            // Load categories
            var dbCategories = await _unitOfWork.Categories.GetAllAsync();
            var activeCategories = dbCategories.Where(c => c.IsActive).ToList();
            
            // Add "All" category at the beginning
            var allCategory = new Category 
            { 
                Id = 0, 
                Name = Localization.GetString("Action_All") ?? "All",
                NameAr = "الكل" 
            };
            
            activeCategories.Insert(0, allCategory);
            Categories = new ObservableCollection<Category>(activeCategories);

            // Default select "All" if nothing selected
            if (SelectedCategory == null)
            {
                SelectedCategory = allCategory;
            }

            // Calculate stats
            await CalculateStatsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        FilterByCategoryCommand.Execute(null);
    }

    private async Task CalculateStatsAsync()
    {
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        var activeProducts = allProducts.Where(p => p.IsActive).ToList();

        TotalProducts = activeProducts.Count;
        LowStockCount = activeProducts.Count(p => p.Stock > 0 && p.Stock <= p.MinStock);
        OutOfStockCount = activeProducts.Count(p => p.Stock <= 0);
        TotalInventoryValue = activeProducts.Sum(p => p.Stock * p.CostPrice);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task FilterLowStockAsync()
    {
        ShowLowStockOnly = !ShowLowStockOnly;
        ShowOutOfStockOnly = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task FilterOutOfStockAsync()
    {
        ShowOutOfStockOnly = !ShowOutOfStockOnly;
        ShowLowStockOnly = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync()
    {
        ShowLowStockOnly = false;
        ShowOutOfStockOnly = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ClearCategoryFilterAsync()
    {
        SelectedCategory = null;
        await LoadDataAsync();
    }

    [RelayCommand]
    private void OpenAdjustStockDialog(Product product)
    {
        if (product == null) return;

        SelectedProduct = product;
        AdjustmentQuantity = 0;
        AdjustmentType = "Add";
        AdjustmentNotes = string.Empty;
        ShowAdjustStockDialog = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        ShowAdjustStockDialog = false;
        AdjustmentQuantity = 0;
        AdjustmentNotes = string.Empty;
    }

    [RelayCommand]
    private async Task AdjustStockAsync()
    {
        if (SelectedProduct == null || AdjustmentQuantity <= 0) return;

        try
        {
            decimal quantity = AdjustmentType == "Add" ? AdjustmentQuantity : -AdjustmentQuantity;
            
            await _unitOfWork.Products.UpdateStockAsync(SelectedProduct.Id, quantity);

            CloseDialog();
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Handle error
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadDataAsync();
            return;
        }

        var results = await _unitOfWork.Products.SearchAsync(SearchText);
        Products = new ObservableCollection<Product>(results.Where(p => p.IsActive));
    }
}
