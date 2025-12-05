using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IPdfReportGenerator
{
    Task<ServiceResult<byte[]>> GenerateSalesReportPdfAsync(SalesReportDto data, string title);
    Task<ServiceResult<byte[]>> GenerateConsignorPerformanceReportPdfAsync(ConsignorPerformanceReportDto data, string title);
    Task<ServiceResult<byte[]>> GenerateInventoryAgingReportPdfAsync(InventoryAgingReportDto data, string title);
    Task<ServiceResult<byte[]>> GeneratePayoutSummaryReportPdfAsync(PayoutSummaryReportDto data, string title);
    Task<ServiceResult<byte[]>> GenerateDailyReconciliationReportPdfAsync(DailyReconciliationDto data, string title);
}