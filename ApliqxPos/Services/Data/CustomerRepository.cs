using Microsoft.EntityFrameworkCore;
using ApliqxPos.Data;
using ApliqxPos.Models;

namespace ApliqxPos.Services.Data;

/// <summary>
/// Customer repository with debt management.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetCustomersWithDebtAsync();
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<decimal> GetTotalDebtAsync(int customerId);
    Task UpdateDebtAsync(int customerId, decimal amount);
}

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Customer>> GetCustomersWithDebtAsync()
    {
        var customers = await _dbSet
            .Where(c => c.CurrentDebt > 0 || c.Sales.Any(s => s.TotalAmount - s.DiscountAmount > s.PaidAmount))
            .ToListAsync();
            
        return customers.OrderByDescending(c => c.CurrentDebt);
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Where(c => c.Name.ToLower().Contains(term) || 
                       (c.Phone != null && c.Phone.Contains(term)))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Phone == phone);
    }

    public async Task<decimal> GetTotalDebtAsync(int customerId)
    {
        var customer = await GetByIdAsync(customerId);
        return customer?.CurrentDebt ?? 0;
    }

    public async Task UpdateDebtAsync(int customerId, decimal amount)
    {
        var customer = await GetByIdAsync(customerId);
        if (customer != null)
        {
            customer.CurrentDebt += amount;
            await UpdateAsync(customer);
        }
    }

    public override async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
