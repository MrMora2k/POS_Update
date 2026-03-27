using System.Collections.ObjectModel;
using ApliqxPos.Data;
using ApliqxPos.Models;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ApliqxPos.ViewModels;

public partial class DebtsViewModel : ObservableObject
{
    private readonly ICustomerRepository _customerRepository;

    [ObservableProperty]
    private ObservableCollection<Customer> _debtors = new();

    [ObservableProperty]
    private Customer? _selectedDebtor;

    [ObservableProperty]
    private decimal _totalDebt;

    [ObservableProperty]
    private bool _isLoading;

    // Payment Dialog
    [ObservableProperty]
    private bool _isPaymentDialogOpen;

    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private string _paymentNotes = string.Empty;

    public DebtsViewModel()
    {
        try
        {
            var context = new ApliqxPos.Data.AppDbContext();
            _customerRepository = new CustomerRepository(context);
           // Execute safely without awaiting
            _ = Task.Run(async () => 
            {
                try 
                { 
                    await Task.Delay(100);
                    await Application.Current.Dispatcher.InvokeAsync(async () => await LoadDataAsync());
                } 
                catch { } 
            });
        }
        catch (Exception)
        {
            _customerRepository = null!;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var debtors = await _customerRepository.GetCustomersWithDebtAsync();
            Debtors = new ObservableCollection<Customer>(debtors);
            TotalDebt = Debtors.Sum(c => c.CurrentDebt);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Add Debt Dialog
    [ObservableProperty]
    private bool _isAddDebtDialogOpen;

    [ObservableProperty]
    private ObservableCollection<Customer> _allCustomers = new();

    [ObservableProperty]
    private Customer? _selectedCustomerForDebt;

    [ObservableProperty]
    private decimal _newDebtAmount;

    [ObservableProperty]
    private string _newDebtNotes = string.Empty;

    // New Customer Mode
    [ObservableProperty]
    private bool _isNewCustomerMode;

    [ObservableProperty]
    private string _newCustomerName = string.Empty;

    [ObservableProperty]
    private string _newCustomerPhone = string.Empty;

    [RelayCommand]
    private async Task OpenAddDebtDialogAsync()
    {
        IsLoading = true;
        try
        {
            var customers = await _customerRepository.GetAllAsync();
            AllCustomers = new ObservableCollection<Customer>(customers);
            
            SelectedCustomerForDebt = null;
            NewDebtAmount = 0;
            NewDebtNotes = string.Empty;
            
            // Reset New Customer fields
            IsNewCustomerMode = false;
            NewCustomerName = string.Empty;
            NewCustomerPhone = string.Empty;

            IsAddDebtDialogOpen = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CloseAddDebtDialog()
    {
        IsAddDebtDialogOpen = false;
        SelectedCustomerForDebt = null;
    }

    [RelayCommand]
    private async Task SaveDebtAsync()
    {
        if (NewDebtAmount <= 0) return;

        try
        {
            if (IsNewCustomerMode)
            {
                // Validate New Customer
                if (string.IsNullOrWhiteSpace(NewCustomerName)) return;

                var newCustomer = new Customer
                {
                    Name = NewCustomerName,
                    Phone = NewCustomerPhone,
                    CurrentDebt = NewDebtAmount
                };

                await _customerRepository.AddAsync(newCustomer);
            }
            else
            {
                // Validate Existing Customer
                if (SelectedCustomerForDebt == null) return;

                // Adding debt means increasing CurrentDebt
                await _customerRepository.UpdateDebtAsync(SelectedCustomerForDebt.Id, NewDebtAmount);
            }
            
            CloseAddDebtDialog();
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Handle error
        }
    }

    [RelayCommand]
    private void OpenPayDialog(Customer customer)
    {
        if (customer == null) return;
        SelectedDebtor = customer;
        PaymentAmount = 0;
        PaymentNotes = string.Empty;
        IsPaymentDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsPaymentDialogOpen = false;
        SelectedDebtor = null;
    }

    [RelayCommand]
    private async Task PayDebtAsync()
    {
        if (SelectedDebtor == null || PaymentAmount <= 0) return;

        try
        {
            // Paying debt means decreasing CurrentDebt (negative value to update)
            await _customerRepository.UpdateDebtAsync(SelectedDebtor.Id, -PaymentAmount);
            
            CloseDialog();
            await LoadDataAsync();
        }
        catch (Exception)
        {
            // Handle error
        }
    }
}
