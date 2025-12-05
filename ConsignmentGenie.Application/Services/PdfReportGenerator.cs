using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConsignmentGenie.Application.Services;

public class PdfReportGenerator : IPdfReportGenerator
{
    public PdfReportGenerator()
    {
        // Configure QuestPDF for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ServiceResult<byte[]>> GenerateSalesReportPdfAsync(SalesReportDto data, string title)
    {
        try
        {
            var pdf = GenerateSalesReportPdf(data, title);
            return ServiceResult<byte[]>.SuccessResult(pdf);
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate PDF", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> GenerateConsignorPerformanceReportPdfAsync(ConsignorPerformanceReportDto data, string title)
    {
        try
        {
            var pdf = GenerateConsignorPerformancePdf(data, title);
            return ServiceResult<byte[]>.SuccessResult(pdf);
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate PDF", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> GenerateInventoryAgingReportPdfAsync(InventoryAgingReportDto data, string title)
    {
        try
        {
            var pdf = GenerateInventoryAgingPdf(data, title);
            return ServiceResult<byte[]>.SuccessResult(pdf);
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate PDF", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> GeneratePayoutSummaryReportPdfAsync(PayoutSummaryReportDto data, string title)
    {
        try
        {
            var pdf = GeneratePayoutSummaryPdf(data, title);
            return ServiceResult<byte[]>.SuccessResult(pdf);
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate PDF", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> GenerateDailyReconciliationReportPdfAsync(DailyReconciliationDto data, string title)
    {
        try
        {
            var pdf = GenerateDailyReconciliationPdf(data);
            return ServiceResult<byte[]>.SuccessResult(pdf);
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate PDF", new List<string> { ex.Message });
        }
    }

    private static byte[] GenerateSalesReportPdf(SalesReportDto data, string title)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Sales Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text(title).FontSize(12);
                        column.Item().PaddingVertical(5);

                        // Summary metrics
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Text("Total Sales").FontSize(10);
                                col.Item().Text($"${data.TotalSales:F2}").FontSize(16).SemiBold();
                            });

                            row.RelativeItem().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Text("Shop Revenue").FontSize(10);
                                col.Item().Text($"${data.ShopRevenue:F2}").FontSize(16).SemiBold();
                            });

                            row.RelativeItem().Border(1).Padding(10).Column(col =>
                            {
                                col.Item().Text("Consignor Payable").FontSize(10);
                                col.Item().Text($"${data.ConsignorPayable:F2}").FontSize(16).SemiBold();
                            });
                        });

                        column.Item().PaddingVertical(10);

                        // Transactions table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).Text("Item");
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Amount");
                                header.Cell().Element(CellStyle).Text("Payment");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var transaction in data.Transactions.Take(50)) // Limit for PDF
                            {
                                table.Cell().Element(CellStyle).Text(transaction.Date.ToString("MM/dd/yyyy"));
                                table.Cell().Element(CellStyle).Text(transaction.ItemName);
                                table.Cell().Element(CellStyle).Text(transaction.ConsignorName);
                                table.Cell().Element(CellStyle).Text($"${transaction.SalePrice:F2}");
                                table.Cell().Element(CellStyle).Text(transaction.PaymentMethod);

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GenerateConsignorPerformancePdf(ConsignorPerformanceReportDto data, string title)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Consignor Performance Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text(title).FontSize(12);
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Consigned");
                                header.Cell().Element(CellStyle).Text("Sold");
                                header.Cell().Element(CellStyle).Text("Available");
                                header.Cell().Element(CellStyle).Text("Sales");
                                header.Cell().Element(CellStyle).Text("Sell %");
                                header.Cell().Element(CellStyle).Text("Pending");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var provider in data.Consignors)
                            {
                                table.Cell().Element(CellStyle).Text(provider.ConsignorName);
                                table.Cell().Element(CellStyle).Text(provider.ItemsConsigned.ToString());
                                table.Cell().Element(CellStyle).Text(provider.ItemsSold.ToString());
                                table.Cell().Element(CellStyle).Text(provider.ItemsAvailable.ToString());
                                table.Cell().Element(CellStyle).Text($"${provider.TotalSales:F0}");
                                table.Cell().Element(CellStyle).Text($"{provider.SellThroughRate:F0}%");
                                table.Cell().Element(CellStyle).Text($"${provider.PendingPayout:F0}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GenerateInventoryAgingPdf(InventoryAgingReportDto data, string title)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Inventory Aging Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text(title).FontSize(12);
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Item");
                                header.Cell().Element(CellStyle).Text("SKU");
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Price");
                                header.Cell().Element(CellStyle).Text("Listed");
                                header.Cell().Element(CellStyle).Text("Days");
                                header.Cell().Element(CellStyle).Text("Action");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var item in data.Items.Take(50)) // Limit for PDF
                            {
                                table.Cell().Element(CellStyle).Text(item.Name);
                                table.Cell().Element(CellStyle).Text(item.SKU);
                                table.Cell().Element(CellStyle).Text(item.ConsignorName);
                                table.Cell().Element(CellStyle).Text($"${item.Price:F0}");
                                table.Cell().Element(CellStyle).Text(item.ListedDate.ToString("MM/dd/yyyy"));
                                table.Cell().Element(CellStyle).Text(item.DaysListed.ToString());
                                table.Cell().Element(CellStyle).Text(item.SuggestedAction);

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GeneratePayoutSummaryPdf(PayoutSummaryReportDto data, string title)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Payout Summary Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text(title).FontSize(12);
                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Consignor");
                                header.Cell().Element(CellStyle).Text("Sales");
                                header.Cell().Element(CellStyle).Text("Cut");
                                header.Cell().Element(CellStyle).Text("Paid");
                                header.Cell().Element(CellStyle).Text("Pending");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var provider in data.Consignors)
                            {
                                table.Cell().Element(CellStyle).Text(provider.ConsignorName);
                                table.Cell().Element(CellStyle).Text($"${provider.TotalSales:F0}");
                                table.Cell().Element(CellStyle).Text($"${provider.ConsignorCut:F0}");
                                table.Cell().Element(CellStyle).Text($"${provider.AlreadyPaid:F0}");
                                table.Cell().Element(CellStyle).Text($"${provider.PendingBalance:F0}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }

    private static byte[] GenerateDailyReconciliationPdf(DailyReconciliationDto data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header()
                    .Text("Daily Reconciliation Report")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Item().Text($"Date: {data.Date:yyyy-MM-dd}").FontSize(12);
                        column.Item().PaddingVertical(10);

                        // Summary section
                        column.Item().Border(1).Padding(10).Column(summaryColumn =>
                        {
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Cash Sales: ${data.CashSales:F2}");
                                row.RelativeItem().Text($"Card Sales: ${data.CardSales:F2}");
                            });
                            summaryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Expected Cash: ${data.ExpectedCash:F2}");
                                row.RelativeItem().Text($"Actual Cash: ${data.ActualCash:F2}");
                            });
                            if (data.Variance.HasValue)
                            {
                                summaryColumn.Item().Text($"Variance: ${data.Variance:F2}").SemiBold();
                            }
                        });

                        column.Item().PaddingVertical(10);

                        // Transactions table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Time");
                                header.Cell().Element(CellStyle).Text("Items");
                                header.Cell().Element(CellStyle).Text("Method");
                                header.Cell().Element(CellStyle).Text("Amount");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            foreach (var transaction in data.Transactions)
                            {
                                table.Cell().Element(CellStyle).Text(transaction.Time.ToString("HH:mm"));
                                table.Cell().Element(CellStyle).Text(transaction.Items);
                                table.Cell().Element(CellStyle).Text(transaction.PaymentMethod);
                                table.Cell().Element(CellStyle).Text($"${transaction.Amount:F2}");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });
                    });
            });
        })
        .GeneratePdf();
    }
}