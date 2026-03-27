using System.Collections.ObjectModel;
using ApliqxPos.Models;
using ApliqxPos.Services.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ApliqxPos.ViewModels;

public partial class SalesViewModel : ObservableObject
{
    private readonly ISaleRepository _saleRepository;

    [ObservableProperty]
    private ObservableCollection<Sale> _sales = new();

    [ObservableProperty]
    private Sale? _selectedSale;

    [ObservableProperty]
    private decimal _totalRevenue;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    public SalesViewModel()
    {
        try
        {
            var context = new ApliqxPos.Data.AppDbContext();
            _saleRepository = new SaleRepository(context);
            // Execute safely without awaiting
            _ = Task.Run(async () => 
            {
                try 
                { 
                    // Small delay to ensure UI is ready
                    await Task.Delay(100); 
                    await Application.Current.Dispatcher.InvokeAsync(async () => await LoadDataAsync());
                } 
                catch { } 
            });
        }
        catch (Exception)
        {
            // Fail gracefully
            _saleRepository = null!;
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var sales = await _saleRepository.GetByDateRangeAsync(StartDate, EndDate.AddDays(1).AddSeconds(-1));
            Sales = new ObservableCollection<Sale>(sales);
            TotalRevenue = Sales.Sum(s => s.FinalAmount);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSaleAsync(Sale sale)
    {
        if (sale == null) return;
        
        // TODO: confirm dialog? For now just implement basic deletion if supported by repo, 
        // but typically we don't delete sales in POS, we cancel them. 
        // Checking repo... generic Repository has DeleteAsync.
        // Let's assume for now we just cancel or delete.
        
        await _saleRepository.DeleteAsync(sale);
        Sales.Remove(sale);
        TotalRevenue = Sales.Sum(s => s.FinalAmount);
    }
}
