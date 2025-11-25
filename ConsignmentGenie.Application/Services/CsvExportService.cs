using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;
using ConsignmentGenie.Application.Services.Interfaces;
using System.Text;

namespace ConsignmentGenie.Application.Services;

public class CsvExportService : ICsvExportService
{
    public async Task<ServiceResult<byte[]>> ExportSalesReportAsync(SalesReportDto data)
    {
        try
        {
            var csv = GenerateSalesReportCsv(data);
            return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate CSV", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportProviderPerformanceReportAsync(ProviderPerformanceReportDto data)
    {
        try
        {
            var csv = GenerateProviderPerformanceCsv(data);
            return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate CSV", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportInventoryAgingReportAsync(InventoryAgingReportDto data)
    {
        try
        {
            var csv = GenerateInventoryAgingCsv(data);
            return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate CSV", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportPayoutSummaryReportAsync(PayoutSummaryReportDto data)
    {
        try
        {
            var csv = GeneratePayoutSummaryCsv(data);
            return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate CSV", new List<string> { ex.Message });
        }
    }

    public async Task<ServiceResult<byte[]>> ExportDailyReconciliationReportAsync(DailyReconciliationDto data)
    {
        try
        {
            var csv = GenerateDailyReconciliationCsv(data);
            return ServiceResult<byte[]>.SuccessResult(Encoding.UTF8.GetBytes(csv));
        }
        catch (Exception ex)
        {
            return ServiceResult<byte[]>.FailureResult("Failed to generate CSV", new List<string> { ex.Message });
        }
    }

    private static string GenerateSalesReportCsv(SalesReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Date,Item Name,Category,Provider Name,Sale Price,Shop Cut,Provider Cut,Payment Method");

        foreach (var transaction in data.Transactions)
        {
            csv.AppendLine($"{transaction.Date:yyyy-MM-dd},{EscapeCsv(transaction.ItemName)},{EscapeCsv(transaction.Category)},{EscapeCsv(transaction.ProviderName)},{transaction.SalePrice:F2},{transaction.ShopCut:F2},{transaction.ProviderCut:F2},{EscapeCsv(transaction.PaymentMethod)}");
        }

        return csv.ToString();
    }

    private static string GenerateProviderPerformanceCsv(ProviderPerformanceReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Provider Name,Items Consigned,Items Sold,Items Available,Total Sales,Sell-Through Rate %,Avg Days to Sell,Pending Payout");

        foreach (var provider in data.Providers)
        {
            csv.AppendLine($"{EscapeCsv(provider.ProviderName)},{provider.ItemsConsigned},{provider.ItemsSold},{provider.ItemsAvailable},{provider.TotalSales:F2},{provider.SellThroughRate:F1},{provider.AvgDaysToSell:F0},{provider.PendingPayout:F2}");
        }

        return csv.ToString();
    }

    private static string GenerateInventoryAgingCsv(InventoryAgingReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Item Name,SKU,Category,Provider Name,Price,Listed Date,Days Listed,Suggested Action");

        foreach (var item in data.Items)
        {
            csv.AppendLine($"{EscapeCsv(item.Name)},{EscapeCsv(item.SKU)},{EscapeCsv(item.Category)},{EscapeCsv(item.ProviderName)},{item.Price:F2},{item.ListedDate:yyyy-MM-dd},{item.DaysListed},{EscapeCsv(item.SuggestedAction)}");
        }

        return csv.ToString();
    }

    private static string GeneratePayoutSummaryCsv(PayoutSummaryReportDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Provider Name,Total Sales,Provider Cut,Already Paid,Pending Balance,Last Payout Date");

        foreach (var provider in data.Providers)
        {
            var lastPayoutDate = provider.LastPayoutDate?.ToString("yyyy-MM-dd") ?? "";
            csv.AppendLine($"{EscapeCsv(provider.ProviderName)},{provider.TotalSales:F2},{provider.ProviderCut:F2},{provider.AlreadyPaid:F2},{provider.PendingBalance:F2},{lastPayoutDate}");
        }

        return csv.ToString();
    }

    private static string GenerateDailyReconciliationCsv(DailyReconciliationDto data)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Time,Transaction ID,Items,Payment Method,Amount");

        foreach (var transaction in data.Transactions)
        {
            csv.AppendLine($"{transaction.Time:HH:mm:ss},{EscapeCsv(transaction.TransactionId)},{EscapeCsv(transaction.Items)},{EscapeCsv(transaction.PaymentMethod)},{transaction.Amount:F2}");
        }

        return csv.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }
}