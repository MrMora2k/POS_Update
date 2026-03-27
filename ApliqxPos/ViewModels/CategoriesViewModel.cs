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
/// ViewModel for Categories management.
/// Handles CRUD operations for product categories.
/// </summary>
public partial class CategoriesViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // ... (keep creating other properties if needed or assume existing are fine, focusing on the changes)

    // Dialog state
    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    // Edit form
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editDescription = string.Empty;

    [ObservableProperty]
    private string _editIcon = "Package";

    [ObservableProperty]
    private bool _isLoading;

    public LocalizationService Localization => LocalizationService.Instance;

    // Available icons for categories
    public string[] AvailableIcons { get; } = new[]
    {
        "Package", "Food", "Bottle", "Coffee", "Cupcake", 
        "Beer", "Pizza", "Hamburger", "IceCream", "Cookie",
        "Water", "Fruit", "Carrot", "Cheese", "Bread",
        "Smoking", "Phone", "Laptop", "Television", "Tablet"
    };

    public CategoriesViewModel()
    {
        var context = new AppDbContext();
        _unitOfWork = new UnitOfWork(context);
        _ = LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        IsLoading = true;
        try
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                categories = categories.Where(c => 
                    c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            Categories = new ObservableCollection<Category>(categories.OrderBy(c => c.Name));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadCategoriesAsync();
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        IsEditMode = false;
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditIcon = "Package";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Category category)
    {
        SelectedCategory = category;
        IsEditMode = true;
        EditName = category.Name;
        EditDescription = category.Description ?? string.Empty;
        EditIcon = category.Icon ?? "Package";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName)) return;

        IsLoading = true;
        try
        {
            if (IsEditMode && SelectedCategory != null)
            {
                // Update existing
                SelectedCategory.Name = EditName;
                SelectedCategory.Description = EditDescription;
                SelectedCategory.Icon = EditIcon;

                await _unitOfWork.Categories.UpdateAsync(SelectedCategory);
            }
            else
            {
                // Create new
                var category = new Category
                {
                    Name = EditName,
                    Description = EditDescription,
                    Icon = EditIcon,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Categories.AddAsync(category);
            }

            await _unitOfWork.SaveChangesAsync();
            IsDialogOpen = false;
            await LoadCategoriesAsync();
            
            // Notify others
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(DataType.Category));
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "No details available";
            System.Windows.MessageBox.Show($"حدث خطأ أثناء حفظ التصنيف: {ex.Message}\n\nالتفاصيل: {innerMessage}", "خطأ في قاعدة البيانات", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            Console.WriteLine($"[ERROR] SaveCategoryAsync: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category category)
    {
        // Check if category has products
        var products = await _unitOfWork.Products.GetByCategoryAsync(category.Id);
        if (products.Any())
        {
            // Cannot delete - has products
            return;
        }

        await _unitOfWork.Categories.DeleteAsync(category);
        await _unitOfWork.SaveChangesAsync();
        await LoadCategoriesAsync();
        
        // Notify others
        WeakReferenceMessenger.Default.Send(new DataChangedMessage(DataType.Category));
    }

    [RelayCommand]
    private void SetIcon(string icon)
    {
        EditIcon = icon;
    }
}
