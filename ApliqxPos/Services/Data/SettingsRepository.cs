using ApliqxPos.Data;
using ApliqxPos.Models;
using Microsoft.EntityFrameworkCore;

namespace ApliqxPos.Services.Data;

public interface ISettingsRepository : IRepository<AppSetting>
{
    Task<string?> GetValueAsync(string key);
    Task SetValueAsync(string key, string value, string? description = null);
    Task<Dictionary<string, string>> GetAllSettingsAsync();
}

public class SettingsRepository : Repository<AppSetting>, ISettingsRepository
{
    public SettingsRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, string? description = null)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new AppSetting { Key = key, Value = value, Description = description };
            await _context.AppSettings.AddAsync(setting);
        }
        else
        {
            setting.Value = value;
            if (description != null) setting.Description = description;
            _context.AppSettings.Update(setting);
        }
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        return await _context.AppSettings.ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty);
    }
}
