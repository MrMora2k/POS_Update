using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Models;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using ApliqxPos.Data;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for Customers management screen.
/// Handles CRUD operations for customers and debt management.
/// </summary>
public partial class CustomersViewModel : ObservableObject, IRecipient<ViewSwitchedMessage>
{
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDialogOpen;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _showDebtPaymentDialog;

    [ObservableProperty]
    private bool _showOnlyWithDebt;

    // Customer form fields
    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private string _customerAddress = string.Empty;

    [ObservableProperty]
    private string _customerNotes = string.Empty;

    [ObservableProperty]
    private decimal _customerDebtLimit;

    // Debt payment fields
    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private string _paymentNotes = string.Empty;

    public LocalizationService Localization => LocalizationService.Instance;

    public CustomersViewModel()
    {
        var context = new AppDbContext();
        _unitOfWork = new UnitOfWork(context);
        
        WeakReferenceMessenger.Default.Register(this);
        
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            IEnumerable<Customer> customers;
            
            if (ShowOnlyWithDebt)
            {
                customers = await _unitOfWork.Customers.GetCustomersWithDebtAsync();
            }
            else
            {
                customers = await _unitOfWork.Customers.GetAllAsync();
            }

            Customers = new ObservableCollection<Customer>(customers.Where(c => c.IsActive));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ToggleDebtFilterAsync()
    {
        ShowOnlyWithDebt = !ShowOnlyWithDebt;
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
    private void OpenEditDialog(Customer customer)
    {
        if (customer == null) return;

        IsEditMode = true;
        SelectedCustomer = customer;
        
        // Populate form
        CustomerName = customer.Name;
        CustomerPhone = customer.Phone ?? string.Empty;
        CustomerAddress = customer.Address ?? string.Empty;
        CustomerNotes = customer.Notes ?? string.Empty;
        CustomerDebtLimit = customer.DebtLimit;

        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
        ShowDebtPaymentDialog = false;
        ClearForm();
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerName)) return;

        try
        {
            if (IsEditMode && SelectedCustomer != null)
            {
                // Update existing customer
                SelectedCustomer.Name = CustomerName;
                SelectedCustomer.Phone = string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone;
                SelectedCustomer.Address = string.IsNullOrWhiteSpace(CustomerAddress) ? null : CustomerAddress;
                SelectedCustomer.Notes = string.IsNullOrWhiteSpace(CustomerNotes) ? null : CustomerNotes;
                SelectedCustomer.DebtLimit = CustomerDebtLimit;

                await _unitOfWork.Customers.UpdateAsync(SelectedCustomer);
            }
            else
            {
                // Create new customer
                var customer = new Customer
                {
                    Name = CustomerName,
                    Phone = string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone,
                    Address = string.IsNullOrWhiteSpace(CustomerAddress) ? null : CustomerAddress,
                    Notes = string.IsNullOrWhiteSpace(CustomerNotes) ? null : CustomerNotes,
                    DebtLimit = CustomerDebtLimit,
                    CurrentDebt = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Customers.AddAsync(customer);
            }

            CloseDialog();
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Handle error
        }
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer customer)
    {
        if (customer == null) return;

        try
        {
            // Soft delete - just mark as inactive
            customer.IsActive = false;
            await _unitOfWork.Customers.UpdateAsync(customer);
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Handle error
        }
    }

    [RelayCommand]
    private void OpenPayDebtDialog(Customer customer)
    {
        if (customer == null || customer.CurrentDebt <= 0) return;

        SelectedCustomer = customer;
        PaymentAmount = 0;
        PaymentNotes = string.Empty;
        ShowDebtPaymentDialog = true;
    }

    [RelayCommand]
    private async Task PayDebtAsync()
    {
        if (SelectedCustomer == null || PaymentAmount <= 0) return;

        try
        {
            // Create payment record
            var payment = new Payment
            {
                CustomerId = SelectedCustomer.Id,
                Amount = PaymentAmount,
                Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes,
                PaymentDate = DateTime.UtcNow
            };

            // Update customer debt (subtract payment from debt)
            await _unitOfWork.Customers.UpdateDebtAsync(SelectedCustomer.Id, -PaymentAmount);

            CloseDialog();
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Handle error
        }
    }

    [RelayCommand]
    private async Task SearchCustomersAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadDataAsync();
            return;
        }

        var results = await _unitOfWork.Customers.SearchAsync(SearchText);
        Customers = new ObservableCollection<Customer>(results.Where(c => c.IsActive));
    }

    private void ClearForm()
    {
        SelectedCustomer = null;
        CustomerName = string.Empty;
        CustomerPhone = string.Empty;
        CustomerAddress = string.Empty;
        CustomerNotes = string.Empty;
        CustomerDebtLimit = 0;
        PaymentAmount = 0;
        PaymentNotes = string.Empty;
    }

    public void Receive(ViewSwitchedMessage message)
    {
        if (message.Value == "Customers")
        {
            _ = LoadDataAsync();
        }
    }
}
