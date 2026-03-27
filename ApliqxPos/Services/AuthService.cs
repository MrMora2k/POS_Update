using ApliqxPos.Data;
using ApliqxPos.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ApliqxPos.Services;

public class AuthService : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private static readonly Lazy<AuthService> _instance = new(() => new AuthService());
    public static AuthService Instance => _instance.Value;

    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    private AuthService() { }

    public async Task<bool> ProcessLoginAsync(string username, string password)
    {
        using var context = new AppDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return false;

        var inputHash = HashPassword(password);
        if (user.PasswordHash == inputHash)
        {
            CurrentUser = user;
            return true;
        }

        return false;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public async Task<bool> HasOwnerAsync()
    {
        using var context = new AppDbContext();
        return await context.Users.AnyAsync(u => u.Role == UserRole.Owner);
    }

    public async Task RegisterOwnerAsync(string username, string password)
    {
        using var context = new AppDbContext();
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Owner))
        {
            throw new InvalidOperationException("Owner already exists.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = UserRole.Owner,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        CurrentUser = user;
    }

    public async Task CreateUserAsync(string username, string password, UserRole role)
    {
        if (CurrentUser?.Role != UserRole.Owner)
        {
            throw new UnauthorizedAccessException("Only Owner can create users.");
        }

        using var context = new AppDbContext();
        if (await context.Users.AnyAsync(u => u.Username == username))
        {
            throw new InvalidOperationException("Username already taken.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Role = role,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task EnsureDefaultAdminAsync()
    {
        using var context = new AppDbContext();
        if (!await context.Users.AnyAsync())
        {
            var admin = new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin"),
                Role = UserRole.Owner,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        if (CurrentUser?.Role != UserRole.Owner)
        {
            throw new UnauthorizedAccessException("Only Owner can delete users.");
        }

        if (CurrentUser.Id == userId)
        {
            throw new InvalidOperationException("Cannot delete your own account.");
        }

        using var context = new AppDbContext();
        var userToDelete = await context.Users.FindAsync(userId);
        if (userToDelete != null)
        {
            context.Users.Remove(userToDelete);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateUserAsync(User user, string? newPassword)
    {
        if (CurrentUser?.Role != UserRole.Owner)
        {
            throw new UnauthorizedAccessException("Only Owner can update users.");
        }

        using var context = new AppDbContext();
        var existingUser = await context.Users.FindAsync(user.Id);
        if (existingUser != null)
        {
            existingUser.Role = user.Role;
            existingUser.PinCode = user.PinCode;
            
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existingUser.PasswordHash = HashPassword(newPassword);
            }

            await context.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        using var context = new AppDbContext();
        return await context.Users.ToListAsync();
    }

    public async Task SyncOwnerCredentialsAsync(string username, string password)
    {
        using var context = new AppDbContext();
        var owner = await context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Owner);

        if (owner != null)
        {
            owner.Username = username;
            owner.PasswordHash = HashPassword(password);
        }
        else
        {
            owner = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
                Role = UserRole.Owner,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(owner);
        }

        await context.SaveChangesAsync();
        CurrentUser = owner;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
