using Microsoft.EntityFrameworkCore;
using ApliqxPos.Data;
using ApliqxPos.Models;

namespace ApliqxPos.Services.Data;

/// <summary>
/// Category repository.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetWithProductsAsync(int categoryId);
    Task<IEnumerable<Category>> GetCategoriesWithProductCountAsync();
}

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<Category?> GetWithProductsAsync(int categoryId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesWithProductCountAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public override async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
