namespace ApliqxPos.Models;

/// <summary>
/// Represents a product category.
/// </summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

/// <summary>
/// Represents a product in the inventory.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = "IQD";
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; } = 5;
    public int? CategoryId { get; set; }
    public string? ImagePath { get; set; }
    public bool IsWeighted { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Category? Category { get; set; }
    
    // Computed
    public bool IsLowStock => Stock <= MinStock && Stock > 0;
    public bool IsOutOfStock => Stock <= 0;
    public decimal Profit => SalePrice - CostPrice;
}

/// <summary>
/// Represents a customer.
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public decimal DebtLimit { get; set; } = 0;
    public decimal CurrentDebt { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    // Computed
    public bool HasDebt => CurrentDebt > 0;
    public bool IsOverLimit => CurrentDebt > DebtLimit && DebtLimit > 0;
}

/// <summary>
/// Represents a sale transaction.
/// </summary>
public class Sale
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Debt
    public string Currency { get; set; } = "IQD";
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public string? Notes { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public string? CashierName { get; set; }
    public bool IsPrinted { get; set; }
    
    // Navigation
    public Customer? Customer { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    // Computed
    public decimal FinalAmount => TotalAmount - DiscountAmount;
    public decimal RemainingAmount => FinalAmount - PaidAmount;
    public bool IsFullyPaid => RemainingAmount <= 0;

    // Restaurant Mode Fields
    public OrderType OrderType { get; set; } = OrderType.InStore;
    public string? TableNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? CustomerPhone { get; set; }
}

/// <summary>
/// Order Type Enum
/// </summary>
public enum OrderType
{
    InStore,    // Default for Store Mode
    DineIn,     // Restaurant: Table
    Takeaway,   // Restaurant: Pickup
    Delivery    // Restaurant: Delivery
}

/// <summary>
/// Sale status enum.
/// </summary>
public enum SaleStatus
{
    Pending,
    Completed,
    Cancelled,
    Refunded
}

/// <summary>
/// Represents an item in a sale.
/// </summary>
public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    
    // Navigation
    public Sale? Sale { get; set; }
    public Product? Product { get; set; }
    
    // Computed
    public decimal Subtotal => (UnitPrice * Quantity) - Discount;
}

/// <summary>
/// Represents a debt payment from a customer.
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Customer? Customer { get; set; }
}

/// <summary>
/// Represents a stock movement (addition, reduction, adjustment).
/// </summary>
public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string MovementType { get; set; } = "Add"; // Add, Remove, Adjust, Sale
    public string? Reason { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Product? Product { get; set; }
}

/// <summary>
/// Represents an application setting stored in the database.
/// </summary>
public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}
