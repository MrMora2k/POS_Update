using Microsoft.EntityFrameworkCore;
using ApliqxPos.Data;
using ApliqxPos.Models;

namespace ApliqxPos.Services.Data;

/// <summary>
/// Product repository with specialized queries.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> GetLowStockAsync();
    Task<IEnumerable<Product>> GetOutOfStockAsync();
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    Task UpdateStockAsync(int productId, decimal quantity);
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .Where(p => p.CategoryId == categoryId)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public async Task<IEnumerable<Product>> GetLowStockAsync()
    {
        return await _dbSet
            .Where(p => p.Stock <= p.MinStock && p.Stock > 0)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetOutOfStockAsync()
    {
        return await _dbSet
            .Where(p => p.Stock <= 0)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _dbSet
            .Where(p => p.Name.ToLower().Contains(term) || 
                       (p.Barcode != null && p.Barcode.Contains(term)))
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task UpdateStockAsync(int productId, decimal quantity)
    {
        var product = await GetByIdAsync(productId);
        if (product != null)
        {
            product.Stock += quantity;
            await UpdateAsync(product);
        }
    }

    public override async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
