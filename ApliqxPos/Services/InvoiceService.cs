using ApliqxPos.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApliqxPos.Services;

/// <summary>
/// Service for generating PDF invoices using QuestPDF.
/// </summary>
public class InvoiceService
{
    static InvoiceService()
    {
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public class ReceiptOptions
    {
        public string HeaderText { get; set; } = "";
        public string FooterText { get; set; } = "شكراً لزيارتكم";
        public string LogoPath { get; set; } = "";
        public bool ShowLogo { get; set; } = false;
        public bool ShowCashier { get; set; } = true;
        public bool ShowCustomer { get; set; } = true;
        public bool ShowTax { get; set; } = false;
        public bool ShowDiscount { get; set; } = true;
        public string Width { get; set; } = "80mm";
    }

    /// <summary>
    /// Generates a PDF invoice for a sale (Standard A5).
    /// </summary>
    public byte[] GenerateInvoice(Sale sale, string businessName, string businessPhone, string businessAddress)
    {
        return GenerateDocument(sale, businessName, businessPhone, businessAddress, PageSizes.A5);
    }

    /// <summary>
    /// Generates a PDF receipt/invoice based on selected paper size.
    /// </summary>
    public byte[] GenerateReceiptBytes(Sale sale, string businessName, string businessPhone, string businessAddress, ReceiptOptions options, bool isKitchen = false)
    {
        PageSize pageSize;
        bool isThermal = true;

        switch (options.Width)
        {
            case "A4":
                pageSize = PageSizes.A4;
                isThermal = false;
                break;
            case "A5":
                pageSize = PageSizes.A5;
                isThermal = false;
                break;
            case "58mm":
                pageSize = new PageSize(164f, 842f); // Standard thermal width
                isThermal = true;
                break;
            case "80mm":
            default:
                pageSize = new PageSize(226f, 842f); // Standard thermal width
                isThermal = true;
                break;
        }
        
        return GenerateDocument(sale, businessName, businessPhone, businessAddress, pageSize, isReceipt: isThermal, options: options, isKitchen: isKitchen); 
    }

    private byte[] GenerateDocument(Sale sale, string businessName, string businessPhone, string businessAddress, PageSize pageSize, bool isReceipt = false, ReceiptOptions options = null, bool isKitchen = false)
    {
        options ??= new ReceiptOptions(); // Default if null

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                if (isReceipt)
                {
                    page.ContinuousSize(pageSize.Width);
                    page.Margin(10);
                }

                else
                {
                    page.Size(pageSize);
                    page.Margin(20);
                }
                
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontSize(isReceipt ? 10 : 12).FontFamily("Arial"));

                // Header
                page.Header().Column(column =>
                {
                    if (isKitchen)
                    {
                        column.Item().AlignCenter().Text("طلبية مطبخ").FontSize(16).Bold();
                        column.Item().PaddingTop(5).LineHorizontal(1);
                    }
                    else
                    {
                        // Logo
                        if (options.ShowLogo && !string.IsNullOrEmpty(options.LogoPath) && System.IO.File.Exists(options.LogoPath))
                        {
                            column.Item().AlignCenter().Height(50).Image(options.LogoPath);
                        }

                        // Business Info
                        column.Item().AlignCenter().Text(businessName)
                            .FontSize(isReceipt ? 14 : 20).Bold();
                        
                        if (!string.IsNullOrEmpty(businessPhone))
                        {
                            column.Item().AlignCenter().Text(businessPhone)
                                .FontSize(isReceipt ? 8 : 10);
                        }
                        
                        if (!string.IsNullOrEmpty(businessAddress))
                        {
                            column.Item().AlignCenter().Text(businessAddress)
                                .FontSize(isReceipt ? 8 : 10);
                        }

                        // Custom Header Text
                        if (!string.IsNullOrEmpty(options.HeaderText))
                        {
                            column.Item().PaddingVertical(5).AlignCenter().Text(options.HeaderText)
                                .FontSize(isReceipt ? 9 : 11);
                        }

                        column.Item().PaddingTop(10).LineHorizontal(1);
                    }

                    column.Item().PaddingVertical(5).Row(row =>
                    {
                        if (isReceipt)
                        {
                            row.RelativeItem().Column(c => 
                            {
                                c.Item().Text($"#{sale.InvoiceNumber}").FontSize(10).Bold();
                                c.Item().Text($"{sale.SaleDate:yyyy/MM/dd HH:mm}").FontSize(8);
                                if (!isKitchen && options.ShowCashier && !string.IsNullOrEmpty(sale.CashierName))
                                {
                                    c.Item().Text($"الكاشير: {sale.CashierName}").FontSize(8);
                                }
                            });
                        }
                        else
                        {
                            row.RelativeItem().Text($"فاتورة رقم: {sale.InvoiceNumber}").FontSize(10);
                            row.RelativeItem().AlignLeft().Text($"التاريخ: {sale.SaleDate:yyyy/MM/dd HH:mm}").FontSize(10);
                        }
                    });

                    // Order Details (Customer/Table) - Show on Kitchen Receipt too
                    if (isKitchen || options.ShowCustomer)
                    {
                        // Dine-In Info (Priority for Kitchen)
                        if (sale.OrderType == OrderType.DineIn && !string.IsNullOrEmpty(sale.TableNumber))
                        {
                            column.Item().Text($"طاولة: {sale.TableNumber}").FontSize(14).Bold();
                        }
                        else if (sale.OrderType == OrderType.Delivery)
                        {
                             column.Item().Text("توصيل").FontSize(12).Bold();
                        }
                        else if (sale.OrderType == OrderType.Takeaway)
                        {
                             column.Item().Text("سفري").FontSize(12).Bold();
                        }

                        if (!isKitchen)
                        {
                             // Customer Name
                            if (sale.Customer != null)
                            {
                                column.Item().Text($"الزبون: {sale.Customer.Name}").FontSize(isReceipt ? 9 : 10).Bold();
                            }

                            // Customer Phone (Prioritize specific sale phone, then customer profile phone)
                            var phone = !string.IsNullOrEmpty(sale.CustomerPhone) 
                                        ? sale.CustomerPhone 
                                        : sale.Customer?.Phone;
                                        
                            if (!string.IsNullOrEmpty(phone))
                            {
                                column.Item().Text($"الهاتف: {phone}").FontSize(isReceipt ? 9 : 10);
                            }

                            // Delivery Info
                            if (sale.OrderType == OrderType.Delivery)
                            {
                                if (!string.IsNullOrEmpty(sale.DriverName))
                                {
                                    column.Item().Text($"السائق: {sale.DriverName}").FontSize(isReceipt ? 9 : 10);
                                }
                                
                                if (!string.IsNullOrEmpty(sale.DeliveryAddress))
                                {
                                    column.Item().Text($"العنوان: {sale.DeliveryAddress}").FontSize(isReceipt ? 9 : 10);
                                }
                            }
                        }
                    }
                });

                // Content - Items Table
                page.Content().PaddingVertical(5).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Product name
                            columns.RelativeColumn(1); // Quantity
                            if (!isKitchen)
                            {
                                columns.RelativeColumn(1.5f); // Price
                                columns.RelativeColumn(1.5f); // Total
                            }
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).Padding(2).Text("المنتج").Bold();
                            header.Cell().BorderBottom(1).Padding(2).AlignCenter().Text("عدد").Bold();
                            if (!isKitchen)
                            {
                                header.Cell().BorderBottom(1).Padding(2).AlignLeft().Text("سعر").Bold();
                                header.Cell().BorderBottom(1).Padding(2).AlignLeft().Text("إجمالي").Bold();
                            }
                        });

                        // Item rows
                        if (sale.Items != null)
                        {
                            foreach (var item in sale.Items)
                            {
                                // Product Name
                                var nameText = table.Cell().Padding(2).Text(item.ProductName);
                                if (isKitchen) nameText.FontSize(12).Bold();
                                else nameText.FontSize(10);

                                // Quantity
                                var qtyText = table.Cell().Padding(2).AlignCenter().Text(item.Quantity.ToString());
                                if (isKitchen) qtyText.FontSize(12).Bold();
                                else qtyText.FontSize(10);

                                if (!isKitchen)
                                {
                                    table.Cell().Padding(2).AlignLeft().Text($"{item.UnitPrice:N0}");
                                    table.Cell().Padding(2).AlignLeft().Text($"{item.Subtotal:N0}");
                                }
                            }
                        }
                    });

                    // Notes (Important for Kitchen)
                    if (!string.IsNullOrEmpty(sale.Notes))
                    {
                         column.Item().PaddingTop(5).Text($"ملاحظات: {sale.Notes}").FontSize(isReceipt ? 10 : 11).Bold();
                    }

                    if (!isKitchen)
                    {
                        // Totals
                        column.Item().PaddingTop(5).LineHorizontal(1);
                        
                        column.Item().PaddingVertical(2).Row(row =>
                        {
                            row.RelativeItem().Text("المجموع:").Bold();
                            row.ConstantItem(isReceipt ? 60 : 100).AlignLeft().Text($"{sale.TotalAmount:N0}");
                        });

                        if (sale.DiscountAmount > 0)
                        {
                            column.Item().PaddingVertical(2).Row(row =>
                            {
                                row.RelativeItem().Text("الخصم:");
                                row.ConstantItem(isReceipt ? 60 : 100).AlignLeft().Text($"-{sale.DiscountAmount:N0}");
                            });
                        }

                        column.Item().PaddingTop(2).LineHorizontal(1);

                        column.Item().PaddingVertical(2).Row(row =>
                        {
                            row.RelativeItem().Text("الصافي:").FontSize(isReceipt ? 12 : 14).Bold();
                            row.ConstantItem(isReceipt ? 60 : 100).AlignLeft().Text($"{sale.FinalAmount:N0}").FontSize(isReceipt ? 12 : 14).Bold();
                        });
                    }
                });

                // Footer
                if (!isKitchen)
                {
                    page.Footer().Column(column =>
                    {
                        column.Item().PaddingTop(5).LineHorizontal(1);
                        column.Item().PaddingVertical(5).AlignCenter().Text(options.FooterText).FontSize(isReceipt ? 8 : 10);
                    });
                }
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Generates and saves an invoice/receipt PDF to a file.
    /// </summary>
    public void SaveInvoice(Sale sale, string filePath, string businessName, string businessPhone = "", string businessAddress = "", bool isReceipt = false, ReceiptOptions options = null, bool isKitchen = false)
    {
        options ??= new ReceiptOptions();
        byte[] pdfBytes;
        if (isReceipt)
        {
            pdfBytes = GenerateReceiptBytes(sale, businessName, businessPhone, businessAddress, options, isKitchen);
        }
        else
        {
            pdfBytes = GenerateInvoice(sale, businessName, businessPhone, businessAddress);
        }
        
        System.IO.File.WriteAllBytes(filePath, pdfBytes);
    }

    /// <summary>
    /// Generates an invoice and sends it directly to the printer (or opens PDF).
    /// </summary>
    public void PrintInvoice(Sale sale, string businessName, string businessPhone = "", string businessAddress = "", string printerName = "", bool isReceipt = false, ReceiptOptions options = null, bool isKitchen = false)
    {
        // For direct printing, we save to temp
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{(isKitchen ? "kitchen_" : "invoice_")}{sale.Id}.pdf");
        SaveInvoice(sale, tempPath, businessName, businessPhone, businessAddress, isReceipt, options, isKitchen);
        
        if (!string.IsNullOrEmpty(printerName))
        {
            // Send to specific printer using RawPrinterHelper or PdfPrinting
            // For now, since .NET Core printing is complex, we'll try to use Process.Start with print verb if possible, 
            // or just open it. A more robust solution involves using a PDF printing library.
            // But usually, just opening it is what users expect if we don't have a direct raw driver.
            
            // However, for thermal printers, we often want silent printing.
            // We can use the 'printto' verb with ProcessStartInfo if Acrobat Reader/Edge is installed.
            
            try 
            {
                var p = new System.Diagnostics.Process();
                p.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    Verb = "printto",
                    Arguments = $"\"{printerName}\"",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                p.Start();
                return;
            }
            catch 
            {
                // Fallback to opening
            }
        }

        // Open with default PDF viewer
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        });
    }
}
