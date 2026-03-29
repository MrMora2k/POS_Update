using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ApliqxPos.Models;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using ApliqxPos.Data;
using ApliqxPos.Messages;
using System.Collections.ObjectModel;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for Products management screen.
/// Handles CRUD operations for products.
/// </summary>
public partial class ProductsViewModel : ObservableObject, IRecipient<DataChangedMessage>, IRecipient<ViewSwitchedMessage>
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
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    // Product form fields
    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _productNameAr = string.Empty;

    [ObservableProperty]
    private string _productBarcode = string.Empty;

    [ObservableProperty]
    private string _productDescription = string.Empty;

    [ObservableProperty]
    private decimal _productCostPrice;

    [ObservableProperty]
    private decimal _productSalePrice;

    [ObservableProperty]
    private string _productCurrency = "IQD";

    [ObservableProperty]
    private decimal _productStock;

    [ObservableProperty]
    private decimal _productMinStock = 5;

    [ObservableProperty]
    private int? _productCategoryId;

    [ObservableProperty]
    private bool _productIsWeighted;

    [ObservableProperty]
    private string? _productImagePath;

    public LocalizationService Localization => LocalizationService.Instance;

    public bool IsRestaurantMode => ThemeService.Instance.IsRestaurantMode;

    public ProductsViewModel()
    {
        var context = new AppDbContext();
        _unitOfWork = new UnitOfWork(context);
        
        // Register for messages
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ViewSwitchedMessage>(this);

        // Listen to ThemeService changes
        ThemeService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ThemeService.IsRestaurantMode))
            {
                OnPropertyChanged(nameof(IsRestaurantMode));
            }
        };

        _ = LoadDataAsync();
    }

    public void Receive(DataChangedMessage message)
    {
        // Reload if categories changed (to update dropdown)
        if (message.Value == DataType.Category)
        {
            _ = LoadCategoriesAsync();
        }
        // Reload if products changed from elsewhere (e.g. stock update from POS)
        else if (message.Value == DataType.Product)
        {
        }
    }

    public void Receive(ViewSwitchedMessage message)
    {
        if (message.Value == "Products")
        {
            _ = LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await LoadProductsAsync();
            await LoadCategoriesAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadProductsAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        // Only show active (non-deleted) products
        Products = new ObservableCollection<Product>(products.Where(p => p.IsActive));
        
        // Reset selected category to "All" if it was null
        if (SelectedCategory == null && Categories.Any())
        {
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == 0);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var activeCategories = categories.Where(c => c.IsActive).ToList();
        
        // Add "All" category at the beginning
        var allCategory = new Category 
        { 
            Id = 0, 
            Name = Localization.GetString("Action_All") ?? "All",
            NameAr = "الكل" 
        };
        
        activeCategories.Insert(0, allCategory);
        Categories = new ObservableCollection<Category>(activeCategories);
        
        // Default select "All"
        if (SelectedCategory == null)
        {
            SelectedCategory = allCategory;
        }
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        FilterByCategoryCommand.Execute(null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditMode = false;
        ClearForm();
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Product product)
    {
        if (product == null) return;

        IsEditMode = true;
        SelectedProduct = product;
        
        // Populate form
        ProductName = product.Name;
        ProductNameAr = product.NameAr ?? string.Empty;
        ProductBarcode = product.Barcode ?? string.Empty;
        ProductDescription = product.Description ?? string.Empty;
        ProductCostPrice = product.CostPrice;
        ProductSalePrice = product.SalePrice;
        ProductCurrency = product.Currency;
        ProductStock = product.Stock;
        ProductMinStock = product.MinStock;
        ProductCategoryId = product.CategoryId;
        ProductIsWeighted = product.IsWeighted;
        ProductImagePath = product.ImagePath;

        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
        ClearForm();
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductName)) return;

        try
        {
            if (IsEditMode && SelectedProduct != null)
            {
                // Update existing product
                SelectedProduct.Name = ProductName;
                SelectedProduct.NameAr = string.IsNullOrWhiteSpace(ProductNameAr) ? null : ProductNameAr;
                SelectedProduct.Barcode = string.IsNullOrWhiteSpace(ProductBarcode) ? null : ProductBarcode;
                SelectedProduct.Description = string.IsNullOrWhiteSpace(ProductDescription) ? null : ProductDescription;
                SelectedProduct.CostPrice = ProductCostPrice;
                SelectedProduct.SalePrice = ProductSalePrice;
                SelectedProduct.Currency = ProductCurrency;
                SelectedProduct.Stock = ProductStock;
                SelectedProduct.MinStock = ProductMinStock;
                SelectedProduct.CategoryId = ProductCategoryId;
                SelectedProduct.IsWeighted = ProductIsWeighted;
                SelectedProduct.ImagePath = ProductImagePath;
                SelectedProduct.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Products.UpdateAsync(SelectedProduct);
            }
            else
            {
                // Create new product
                
                // Restaurant Mode: Auto-generate barcode if empty
                string? finalBarcode = string.IsNullOrWhiteSpace(ProductBarcode) ? null : ProductBarcode;
                if (IsRestaurantMode && string.IsNullOrEmpty(finalBarcode))
                {
                    // Generate a simple internal barcode: "R" + timestamp suffix
                    finalBarcode = $"R{DateTime.Now.Ticks.ToString().Substring(12)}";
                }

                // Restaurant Mode: Default stock to 10000 if not specified (Unlimited)
                decimal finalStock = ProductStock;
                decimal finalMinStock = ProductMinStock;
                
                if (IsRestaurantMode && finalStock == 0)
                {
                    finalStock = 10000;
                    finalMinStock = 0; // Disable low stock alert
                }

                var product = new Product
                {
                    Name = ProductName,
                    NameAr = string.IsNullOrWhiteSpace(ProductNameAr) ? null : ProductNameAr,
                    Barcode = finalBarcode, // Use the processed barcode
                    Description = string.IsNullOrWhiteSpace(ProductDescription) ? null : ProductDescription,
                    CostPrice = ProductCostPrice,
                    SalePrice = ProductSalePrice,
                    Currency = ProductCurrency,
                    Stock = finalStock,
                    MinStock = finalMinStock,
                    CategoryId = ProductCategoryId,
                    IsWeighted = ProductIsWeighted,
                    ImagePath = ProductImagePath,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Products.AddAsync(product);
            }

            CloseDialog();
            await LoadProductsAsync();
            
            // Notify others
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(DataType.Product));
        }
        catch (Exception ex)
        {
            // Log error for debugging
            System.Diagnostics.Debug.WriteLine($"SaveProduct Error: {ex.Message}");
            System.Windows.MessageBox.Show($"خطأ في حفظ المنتج: {ex.Message}", "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteProductAsync(Product product)
    {
        if (product == null) return;

        // Confirmation dialog
        var result = System.Windows.MessageBox.Show(
            $"هل أنت متأكد من حذف المنتج '{product.Name}'؟",
            "تأكيد الحذف",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            // Soft delete - just mark as inactive
            product.IsActive = false;
            await _unitOfWork.Products.UpdateAsync(product);
            await LoadProductsAsync();
            
            // Notify others
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(DataType.Product));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"خطأ في حذف المنتج: {ex.Message}", "خطأ", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadProductsAsync();
            return;
        }

        var results = await _unitOfWork.Products.SearchAsync(SearchText);
        Products = new ObservableCollection<Product>(results);
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync()
    {
        if (SelectedCategory == null || SelectedCategory.Id == 0)
        {
            await LoadProductsAsync();
            return;
        }

        var products = await _unitOfWork.Products.GetByCategoryAsync(SelectedCategory.Id);
        Products = new ObservableCollection<Product>(products.Where(p => p.IsActive));
    }

    [RelayCommand]
    private void SelectImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "اختر صورة المنتج",
            Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            ProductImagePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void ClearImage()
    {
        ProductImagePath = null;
    }

    private void ClearForm()
    {
        SelectedProduct = null;
        ProductName = string.Empty;
        ProductNameAr = string.Empty;
        ProductBarcode = string.Empty;
        ProductDescription = string.Empty;
        ProductCostPrice = 0;
        ProductSalePrice = 0;
        ProductCurrency = "IQD";
        ProductStock = 0;
        ProductMinStock = 5;
        ProductCategoryId = null;
        ProductIsWeighted = false;
        ProductImagePath = null;
    }
}
