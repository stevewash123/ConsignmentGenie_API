using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ICsvExportService
{
    Task<ServiceResult<byte[]>> ExportSalesReportAsync(SalesReportDto data);
    Task<ServiceResult<byte[]>> ExportConsignorPerformanceReportAsync(ConsignorPerformanceReportDto data);
    Task<ServiceResult<byte[]>> ExportInventoryAgingReportAsync(InventoryAgingReportDto data);
    Task<ServiceResult<byte[]>> ExportPayoutSummaryReportAsync(PayoutSummaryReportDto data);
    Task<ServiceResult<byte[]>> ExportDailyReconciliationReportAsync(DailyReconciliationDto data);
}