using Microsoft.EntityFrameworkCore;
using ApliqxPos.Data;
using ApliqxPos.Models;

namespace ApliqxPos.Services.Data;

/// <summary>
/// Sale repository with reporting queries.
/// </summary>
public interface ISaleRepository : IRepository<Sale>
{
    Task<IEnumerable<Sale>> GetByCustomerAsync(int customerId);
    Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<Sale>> GetTodaySalesAsync();
    Task<Sale?> GetWithItemsAsync(int saleId);
    Task<decimal> GetTotalSalesAsync(DateTime start, DateTime end);
    Task<decimal> GetTotalProfitAsync(DateTime start, DateTime end);
    Task<int> GetSalesCountAsync(DateTime start, DateTime end);
}

public class SaleRepository : Repository<Sale>, ISaleRepository
{
    public SaleRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Sale>> GetByCustomerAsync(int customerId)
    {
        return await _dbSet
            .Where(s => s.CustomerId == customerId)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _dbSet
            .Where(s => s.SaleDate >= start && s.SaleDate <= end)
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Category)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Sale>> GetTodaySalesAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await GetByDateRangeAsync(today, tomorrow);
    }

    public async Task<Sale?> GetWithItemsAsync(int saleId)
    {
        return await _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == saleId);
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime start, DateTime end)
    {
        return await _dbSet
            .Where(s => s.SaleDate >= start && s.SaleDate <= end && s.Status != SaleStatus.Cancelled)
            .SumAsync(s => (decimal?)(s.TotalAmount - s.DiscountAmount)) ?? 0m;
    }

    public async Task<decimal> GetTotalProfitAsync(DateTime start, DateTime end)
    {
        var sales = await _dbSet
            .Where(s => s.SaleDate >= start && s.SaleDate <= end && s.Status != SaleStatus.Cancelled)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();

        return sales.Sum(s => 
            s.Items.Sum(i => (i.UnitPrice - i.Product.CostPrice) * i.Quantity - i.Discount) 
            - s.DiscountAmount);
    }

    public async Task<int> GetSalesCountAsync(DateTime start, DateTime end)
    {
        return await _dbSet
            .Where(s => s.SaleDate >= start && s.SaleDate <= end && s.Status != SaleStatus.Cancelled)
            .CountAsync();
    }

    public override async Task<IEnumerable<Sale>> GetAllAsync()
    {
        return await _dbSet
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }
}
