using System;
using System.ComponentModel.DataAnnotations;

namespace ApliqxPos.Models;

public enum UserRole
{
    Owner,
    Admin,
    Cashier
}

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Cashier;

    [MaxLength(10)]
    public string? PinCode { get; set; } // For quick login

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
