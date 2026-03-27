using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ApliqxPos.Models;
using ApliqxPos.Services;
using ApliqxPos.Services.Data;
using ApliqxPos.Data;
using ApliqxPos.Messages;
using System.Collections.ObjectModel;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for the Point of Sale screen.
/// Handles product selection, cart management, and checkout.
/// </summary>
public partial class PosViewModel : ObservableObject, IRecipient<DataChangedMessage>, IRecipient<ViewSwitchedMessage>
{
    private IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    private ObservableCollection<CartItem> _cartItems = new();
    public ObservableCollection<CartItem> CartItems
    {
        get => _cartItems;
        set
        {
            if (_cartItems != null)
                _cartItems.CollectionChanged -= OnCartItemsChanged;
            
            if (SetProperty(ref _cartItems, value))
            {
                if (_cartItems != null)
                {
                    _cartItems.CollectionChanged += OnCartItemsChanged;
                    foreach(var item in _cartItems) item.PropertyChanged += OnCartItemPropertyChanged;
                }
                UpdateTotals();
            }
        }
    }

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _discount;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private decimal _changeAmount;

    [ObservableProperty]
    private string _paymentMethod = "Cash";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _currency = "IQD";

    public LocalizationService Localization => LocalizationService.Instance;

    // Sessions
    private List<PosSession> _sessions = new();
    
    [ObservableProperty]
    private int _activeSessionIndex = 0;

    [ObservableProperty]
    private bool _isPrintPreviewOpen;

    [ObservableProperty]
    private bool _isNumericKeypadOpen;

    [ObservableProperty]
    private CartItem? _selectedCartItem;

    private bool _isFirstInput;

    // Restaurant Mode Properties
    public bool IsRestaurantMode => ThemeService.Instance.IsRestaurantMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDineIn))]
    [NotifyPropertyChangedFor(nameof(IsTakeaway))]
    [NotifyPropertyChangedFor(nameof(IsDelivery))]
    private OrderType _orderType = OrderType.InStore;

    public bool IsDineIn => OrderType == OrderType.DineIn;
    public bool IsTakeaway => OrderType == OrderType.Takeaway || OrderType == OrderType.Delivery;
    public bool IsDelivery => OrderType == OrderType.Delivery;

    [ObservableProperty]
    private string _tableNumber = string.Empty;

    [ObservableProperty]
    private string _driverName = string.Empty;

    [ObservableProperty]
    private string _deliveryAddress = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    // Commands
    
    [RelayCommand]
    private void SetOrderType(string type)
    {
        if (Enum.TryParse<OrderType>(type, true, out var result))
        {
            OrderType = result;
        }
    }

    [RelayCommand]
    private void SelectSession(string indexStr)
    {
        if (int.TryParse(indexStr, out int index))
        {
            SwitchSession(index);
        }
    }

    private void SwitchSession(int newIndex)
    {
        if (newIndex < 0 || newIndex >= _sessions.Count || newIndex == ActiveSessionIndex) return;

        // 1. Save current state to the active session object
        SaveCurrentSessionState();

        // 2. Load state from the new session object
        LoadSessionState(newIndex);
        
        // 3. Update index
        ActiveSessionIndex = newIndex;
    }

    private void SaveCurrentSessionState()
    {
        var session = _sessions[ActiveSessionIndex];
        session.CartItems = new ObservableCollection<CartItem>(CartItems);
        session.SelectedCustomer = SelectedCustomer;
        session.Discount = Discount;
        session.PaidAmount = PaidAmount;
        session.PaymentMethod = PaymentMethod;
        
        // Restaurant Mode Data
        session.OrderType = OrderType;
        session.TableNumber = TableNumber;
        session.DriverName = DriverName;
        session.DeliveryAddress = DeliveryAddress;
        session.CustomerPhone = CustomerPhone;
    }

    private void LoadSessionState(int index)
    {
        var session = _sessions[index];
        CartItems = new ObservableCollection<CartItem>(session.CartItems);
        SelectedCustomer = session.SelectedCustomer;
        Discount = session.Discount;
        PaidAmount = session.PaidAmount;
        PaymentMethod = session.PaymentMethod;
        
        // Restaurant Mode Data
        OrderType = session.OrderType;
        TableNumber = session.TableNumber;
        DriverName = session.DriverName;
        DeliveryAddress = session.DeliveryAddress;
        CustomerPhone = session.CustomerPhone;

        UpdateTotals();
    }

    [RelayCommand]
    private async Task OpenDrawerAsync()
    {
        // TODO: Integrate with Printer Service to send Pulse command
        // For now, simple indication
        try 
        {
            // Placeholder for sending ESC/POS Pulse to printer
            // await _printService.OpenDrawer();
            await Task.Delay(100); 
        }
        catch { }
    }

    [RelayCommand]
    private void PrintPreview()
    {
        if (CartItems.Count == 0) return;
        IsPrintPreviewOpen = true;
    }

    [RelayCommand]
    private void ClosePrintPreview()
    {
        IsPrintPreviewOpen = false;
    }

    [RelayCommand]
    private void OpenNumericKeypad(CartItem item)
    {
        SelectedCartItem = item;
        IsNumericKeypadOpen = true;
        _isFirstInput = true; // Mark as first input
    }

    public void KeypadInput(string key)
    {
        if (SelectedCartItem == null) return;

        string current = SelectedCartItem.Quantity.ToString();
        
        if (key == "Clear")
        {
            current = "0";
            _isFirstInput = false;
        }
        else if (key == "Backspace")
        {
            if (_isFirstInput || current.Length <= 1)
                current = "0";
            else
                current = current.Substring(0, current.Length - 1);
            _isFirstInput = false;
        }
        else if (key == ".")
        {
            if (_isFirstInput)
            {
                current = "0.";
                _isFirstInput = false;
            }
            else if (!current.Contains("."))
            {
                current += ".";
            }
        }
        else
        {
            // Digit input
            if (_isFirstInput || current == "0")
            {
                current = key;
                _isFirstInput = false;
            }
            else
            {
                current += key;
            }
        }

        if (decimal.TryParse(current, out decimal result))
        {
            // Check stock if needed
            var product = Products.FirstOrDefault(p => p.Id == SelectedCartItem.ProductId);
            if (product != null && result > product.Stock)
            {
                result = product.Stock;
            }
            
            SelectedCartItem.Quantity = result;
            UpdateTotals();
        }
    }

    private readonly InvoiceService _invoiceService;

    [RelayCommand]
    private async Task PayAndPrintAsync()
    {
        // 1. Checkout (Save Sale)
        var sale = await CheckoutAsync();

        if (sale != null)
        {
            // 2. Print functionality
            try
            {
                var settings = await _unitOfWork.Settings.GetAllSettingsAsync();
                
                string businessName = settings.ContainsKey("BusinessName") ? settings["BusinessName"] : "ApliqX POS";
                string businessPhone = settings.ContainsKey("BusinessPhone") ? settings["BusinessPhone"] : "";
                string businessAddress = settings.ContainsKey("BusinessAddress") ? settings["BusinessAddress"] : "";

                // Print Settings
                string printType = settings.ContainsKey("PrintOutputType") ? settings["PrintOutputType"] : "Printer";
                string printerName = settings.ContainsKey("SelectedPrinter") ? settings["SelectedPrinter"] : "";
                string kitchenPrinterName = settings.ContainsKey("SelectedKitchenPrinter") ? settings["SelectedKitchenPrinter"] : "";
                string pdfPath = settings.ContainsKey("PdfSavePath") ? settings["PdfSavePath"] : "";
                string receiptWidth = settings.ContainsKey("ReceiptWidth") ? settings["ReceiptWidth"] : "80mm";
                
                // Determine if receipt format
                bool isReceipt = true; // Default to thermal style
                
                // Advanced Settings construction
                var options = new ApliqxPos.Services.InvoiceService.ReceiptOptions
                {
                    Width = receiptWidth,
                    HeaderText = settings.ContainsKey("PrintHeaderCustomText") ? settings["PrintHeaderCustomText"] : "",
                    FooterText = settings.ContainsKey("PrintFooterCustomText") ? settings["PrintFooterCustomText"] : "شكراً لزيارتكم",
                    LogoPath = settings.ContainsKey("PrintLogoPath") ? settings["PrintLogoPath"] : "",
                    ShowLogo = settings.ContainsKey("PrintShowLogo") && bool.Parse(settings["PrintShowLogo"]),
                    ShowCashier = !settings.ContainsKey("PrintShowCashierName") || bool.Parse(settings["PrintShowCashierName"]),
                    ShowCustomer = !settings.ContainsKey("PrintShowCustomerInfo") || bool.Parse(settings["PrintShowCustomerInfo"]),
                    ShowTax = settings.ContainsKey("PrintShowTaxDetails") && bool.Parse(settings["PrintShowTaxDetails"]),
                    ShowDiscount = !settings.ContainsKey("PrintShowDiscount") || bool.Parse(settings["PrintShowDiscount"])
                };

                // Local function to execute print/save
                void ExecutePrint(bool isKitchen)
                {
                    if (printType == "PDF" && !string.IsNullOrEmpty(pdfPath) && System.IO.Directory.Exists(pdfPath))
                    {
                        // Save as PDF
                        string prefix = isKitchen ? "Kitchen_" : "Invoice_";
                        string fileName = $"{prefix}{sale.InvoiceNumber}_{DateTime.Now:yyyyMMddHHmm}.pdf";
                        string fullPath = System.IO.Path.Combine(pdfPath, fileName);
                        
                        _invoiceService.SaveInvoice(sale, fullPath, businessName, businessPhone, businessAddress, isReceipt, options, isKitchen);
                        
                        // Open the file (Customer copy only to avoid double opening/confusion)
                        if (!isKitchen)
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = fullPath,
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        // Send to Printer
                        // If Kitchen, use Kitchen Printer if available, else fallback to Main Printer
                        string targetPrinter = isKitchen && !string.IsNullOrWhiteSpace(kitchenPrinterName) 
                            ? kitchenPrinterName 
                            : printerName;

                        if (!string.IsNullOrWhiteSpace(targetPrinter))
                        {
                            _invoiceService.PrintInvoice(sale, businessName, businessPhone, businessAddress, targetPrinter, isReceipt, options, isKitchen);
                        }
                    }
                }

                // 1. Print Customer Receipt
                ExecutePrint(false);

                // 2. Print Kitchen Receipt (Restaurant Mode Only)
                if (IsRestaurantMode)
                {
                    // Small delay to ensure order if using same printer/spooler
                    await Task.Delay(500);
                    ExecutePrint(true);
                }

                if (printType == "PDF")
                {
                     System.Windows.MessageBox.Show("تم حفظ الفاتورة بنجاح", "تم الحفظ");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"فشلت الطباعة: {ex.Message}", "خطأ في الطباعة");
            }
        }
    }

    public PosViewModel()
    {
        var context = new AppDbContext();
        _unitOfWork = new UnitOfWork(context);
        _invoiceService = new InvoiceService();
        
        WeakReferenceMessenger.Default.Register<DataChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ViewSwitchedMessage>(this);

        for (int i = 0; i < 4; i++)
        {
            _sessions.Add(new PosSession());
        }

        _ = LoadDataAsync();
        
        CartItems.CollectionChanged += OnCartItemsChanged;
        
        // Subscribe to ThemeService changes to update UI when Restaurant Mode is toggled
        ThemeService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ThemeService.IsRestaurantMode))
            {
                OnPropertyChanged(nameof(IsRestaurantMode));
            }
        };
    }

    public void Receive(DataChangedMessage message)
    {
        if (message.Value == DataType.Product || message.Value == DataType.Category || message.Value == DataType.Customer)
        {
            // Create fresh context to get updated data from database
            var freshContext = new AppDbContext();
            _unitOfWork = new UnitOfWork(freshContext);
            _ = LoadDataAsync();
        }
    }

    public void Receive(ViewSwitchedMessage message)
    {
        if (message.Value == "Pos")
        {
            var freshContext = new AppDbContext();
            _unitOfWork = new UnitOfWork(freshContext);
            _ = LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            Products = new ObservableCollection<Product>(products.Where(p => p.IsActive && p.Stock > 0));

            var categories = await _unitOfWork.Categories.GetAllAsync();
            Categories = new ObservableCollection<Category>(categories.Where(c => c.IsActive));

            var customers = await _unitOfWork.Customers.GetAllAsync();
            Customers = new ObservableCollection<Customer>(customers.Where(c => c.IsActive));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshProductsAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        if (product == null || product.Stock <= 0) return;

        var existingItem = CartItems.FirstOrDefault(c => c.ProductId == product.Id);
        if (existingItem != null)
        {
            if (existingItem.Quantity < product.Stock)
            {
                existingItem.Quantity++;
                OnPropertyChanged(nameof(CartItems));
            }
        }
        else
        {
            CartItems.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.SalePrice,
                Quantity = 1,
                Currency = product.Currency
            });
        }

        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveFromCart(CartItem item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        UpdateTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        if (item == null) return;
        
        var product = Products.FirstOrDefault(p => p.Id == item.ProductId);
        if (product != null && item.Quantity < product.Stock)
        {
            item.Quantity++;
            OnPropertyChanged(nameof(CartItems));
            UpdateTotals();
        }
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItem item)
    {
        if (item == null) return;

        if (item.Quantity > 1)
        {
            item.Quantity--;
            OnPropertyChanged(nameof(CartItems));
            UpdateTotals();
        }
        else
        {
            CartItems.Remove(item);
        }
        UpdateTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        Discount = 0;
        PaidAmount = 0;
        SelectedCustomer = null;
        UpdateTotals();
    }

    [RelayCommand]
    private async Task<Sale?> CheckoutAsync()
    {
        if (CartItems.Count == 0) return null;

        // Auto-fill PaidAmount for Cash sales if left at 0
        if (PaidAmount == 0 && PaymentMethod == "Cash")
        {
            PaidAmount = Total;
        }

        // Prevent debt for anonymous customers
        if (SelectedCustomer == null && PaidAmount < Total)
        {
            PaidAmount = Total;
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Create sale
            var sale = new Sale
            {
                InvoiceNumber = GenerateInvoiceNumber(),
                CustomerId = SelectedCustomer?.Id,
                Customer = SelectedCustomer,
                CashierName = "Admin", // TODO: Get from auth service
                TotalAmount = Subtotal,
                DiscountAmount = Discount,
                PaidAmount = PaidAmount,
                PaymentMethod = PaymentMethod,
                Currency = Currency,
                Status = Total <= PaidAmount ? SaleStatus.Completed : SaleStatus.Pending,
                SaleDate = DateTime.Now,
                Items = new List<SaleItem>(),
                
                // Restaurant Mode Fields
                OrderType = IsRestaurantMode ? OrderType : OrderType.InStore,
                TableNumber = IsDineIn ? TableNumber : null,
                DriverName = IsDelivery ? DriverName : null,
                DeliveryAddress = IsDelivery ? DeliveryAddress : null,
                CustomerPhone = IsDelivery ? CustomerPhone : null
            };

            await _unitOfWork.Sales.AddAsync(sale);

            // Add sale items and update stock
            foreach (var cartItem in CartItems)
            {
                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.ProductName,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    Discount = cartItem.Discount
                };
                
                sale.Items.Add(saleItem); 

                await _unitOfWork.Products.UpdateStockAsync(cartItem.ProductId, -cartItem.Quantity);
            }

            // Update customer debt if partial payment
            if (SelectedCustomer != null && Total > PaidAmount)
            {
                var debtAmount = Total - PaidAmount;
                await _unitOfWork.Customers.UpdateDebtAsync(SelectedCustomer.Id, debtAmount);
            }

            await _unitOfWork.CommitTransactionAsync();

            // Clear cart & Reset inputs
            ClearCart();
            await LoadDataAsync();
            
            // Notify others
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(DataType.Product));
            WeakReferenceMessenger.Default.Send(new DataChangedMessage(DataType.Sale));

            return sale;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    [RelayCommand]
    private void SetPaymentMethod(string method)
    {
        PaymentMethod = method;
    }

    [RelayCommand]
    private void ApplyDiscount(decimal discountAmount)
    {
        Discount = Math.Max(0, Math.Min(discountAmount, Subtotal));
        UpdateTotals();
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadDataAsync();
            return;
        }

        var results = await _unitOfWork.Products.SearchAsync(SearchText);
        var activeResults = results.Where(p => p.IsActive && p.Stock > 0).ToList();

        // Feature: Auto-add to cart on exact barcode scan
        var exactBarcodeMatch = activeResults.FirstOrDefault(p => string.Equals(p.Barcode, SearchText, StringComparison.OrdinalIgnoreCase));
        if (exactBarcodeMatch != null)
        {
            AddToCart(exactBarcodeMatch);
            SearchText = string.Empty;
            // Clear the search results visually and reset product list
            await LoadDataAsync();
            return;
        }

        Products = new ObservableCollection<Product>(activeResults);
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync(Category? category)
    {
        // If no category passed, or if clicking the already selected category (toggle off)
        if (category == null || (SelectedCategory != null && SelectedCategory.Id == category.Id))
        {
            SelectedCategory = null;
            await LoadDataAsync();
            return;
        }

        SelectedCategory = category;
        var products = await _unitOfWork.Products.GetByCategoryAsync(SelectedCategory.Id);
        Products = new ObservableCollection<Product>(products.Where(p => p.IsActive && p.Stock > 0));
    }

    private void UpdateTotals()
    {
        Subtotal = CartItems.Sum(c => c.Subtotal);
        Total = Subtotal - Discount;
        ChangeAmount = PaidAmount > Total ? PaidAmount - Total : 0;
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        UpdateTotals();
    }

    partial void OnDiscountChanged(decimal value)
    {
        UpdateTotals();
    }

    private void OnCartItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CartItem item in e.NewItems)
                item.PropertyChanged += OnCartItemPropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (CartItem item in e.OldItems)
                item.PropertyChanged -= OnCartItemPropertyChanged;
        }
        
        UpdateTotals();
    }

    private void OnCartItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.Subtotal))
        {
            UpdateTotals();
        }
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
    }
}

/// <summary>
/// Represents an item in the shopping cart.
/// </summary>
public partial class CartItem : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _quantity = 1;
    
    public decimal Discount { get; set; }
    public string Currency { get; set; } = "IQD";
    
    public decimal Subtotal => (UnitPrice * Quantity) - Discount;
}

public class PosSession
{
    public ObservableCollection<CartItem> CartItems { get; set; } = new();
    public Customer? SelectedCustomer { get; set; }
    public decimal Discount { get; set; }
    public decimal PaidAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    
    // Restaurant data
    public OrderType OrderType { get; set; } = OrderType.InStore;
    public string TableNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
}


