using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IReportsService
{
    Task<ServiceResult<SalesReportDto>> GetSalesReportAsync(Guid organizationId, SalesReportFilterDto filter);
    Task<ServiceResult<ProviderPerformanceReportDto>> GetProviderPerformanceReportAsync(Guid organizationId, ProviderPerformanceFilterDto filter);
    Task<ServiceResult<InventoryAgingReportDto>> GetInventoryAgingReportAsync(Guid organizationId, InventoryAgingFilterDto filter);
    Task<ServiceResult<PayoutSummaryReportDto>> GetPayoutSummaryReportAsync(Guid organizationId, PayoutSummaryFilterDto filter);
    Task<ServiceResult<DailyReconciliationDto>> GetDailyReconciliationReportAsync(Guid organizationId, DateOnly date);
    Task<ServiceResult<DailyReconciliationDto>> SaveDailyReconciliationAsync(Guid organizationId, DailyReconciliationRequestDto request);

    // Additional Analytics methods (from AnalyticsController)
    Task<ServiceResult<TrendsReportDto>> GetTrendsReportAsync(Guid organizationId, TrendsFilterDto filter);
    Task<ServiceResult<InventoryOverviewDto>> GetInventoryOverviewAsync(Guid organizationId);

    // Export methods
    Task<ServiceResult<byte[]>> ExportSalesReportAsync(Guid organizationId, SalesReportFilterDto filter, string format);
    Task<ServiceResult<byte[]>> ExportProviderPerformanceReportAsync(Guid organizationId, ProviderPerformanceFilterDto filter, string format);
    Task<ServiceResult<byte[]>> ExportInventoryAgingReportAsync(Guid organizationId, InventoryAgingFilterDto filter, string format);
    Task<ServiceResult<byte[]>> ExportPayoutSummaryReportAsync(Guid organizationId, PayoutSummaryFilterDto filter, string format);
    Task<ServiceResult<byte[]>> ExportDailyReconciliationReportAsync(Guid organizationId, DateOnly date, string format);
}