using ApliqxPos.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace ApliqxPos.Services;

public class AuditItem
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal SoldQuantity { get; set; }
    public decimal RemainingStock { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal InventoryValue { get; set; }
}

public class AuditService
{
    static AuditService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateStockAuditPdf(
        IEnumerable<AuditItem> items, 
        string businessName, 
        string periodName, 
        DateTime startDate, 
        DateTime endDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(businessName).FontSize(20).Bold().FontColor(Colors.Indigo.Medium);
                        col.Item().Text(LocalizationService.Instance.GetString("Report_AuditTitle")).FontSize(14).SemiBold();
                    });

                    row.RelativeItem().AlignLeft().Column(col =>
                    {
                        col.Item().Text($"{periodName}").FontSize(12).Bold();
                        col.Item().Text($"{startDate:yyyy/MM/dd} - {endDate:yyyy/MM/dd}").FontSize(10);
                    });
                });

                // Content
                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // Name
                            columns.RelativeColumn(2); // Category
                            columns.RelativeColumn(1.5f); // Sold
                            columns.RelativeColumn(1.5f); // Remaining
                            columns.RelativeColumn(2); // Total Value (Sold)
                        });

                        // Header Row
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text(LocalizationService.Instance.GetString("Label_Name"));
                            header.Cell().Element(HeaderStyle).Text(LocalizationService.Instance.GetString("Product_Category"));
                            header.Cell().Element(HeaderStyle).AlignCenter().Text(LocalizationService.Instance.GetString("Header_SoldQty"));
                            header.Cell().Element(HeaderStyle).AlignCenter().Text(LocalizationService.Instance.GetString("Header_RemainingStock"));
                            header.Cell().Element(HeaderStyle).AlignLeft().Text(LocalizationService.Instance.GetString("Sale_Amount"));

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            }
                        });

                        // Data Rows
                        foreach (var item in items)
                        {
                            table.Cell().Element(CellStyle).Text(item.Name);
                            table.Cell().Element(CellStyle).Text(item.Category);
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.SoldQuantity.ToString("N0"));
                            table.Cell().Element(CellStyle).AlignCenter().Text(item.RemainingStock.ToString("N0"));
                            table.Cell().Element(CellStyle).AlignLeft().Text(item.TotalSalesValue.ToString("N0"));

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                            }
                        }
                    });

                    // Totals Section
                    col.Item().PaddingTop(1, Unit.Centimetre).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(200).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("إجمالي المبيعات:").Bold();
                                r.RelativeItem().AlignLeft().Text($"{items.Sum(i => i.TotalSalesValue):N0} د.ع");
                            });
                            c.Item().PaddingVertical(5).LineHorizontal(1);
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("قيمة المخزن المتبقي:").Bold();
                                r.RelativeItem().AlignLeft().Text($"{items.Sum(i => i.InventoryValue):N0} د.ع");
                            });
                        });
                    });
                });

                // Footer
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }
}
