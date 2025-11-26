using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISalesReportService
{
    Task<ServiceResult<SalesReportDto>> GetSalesReportAsync(Guid organizationId, SalesReportFilterDto filter);
    Task<ServiceResult<TrendsReportDto>> GetTrendsReportAsync(Guid organizationId, TrendsFilterDto filter);
    Task<ServiceResult<DailyReconciliationDto>> GetDailyReconciliationReportAsync(Guid organizationId, DateOnly date);
    Task<ServiceResult<DailyReconciliationDto>> SaveDailyReconciliationAsync(Guid organizationId, DailyReconciliationRequestDto request);
}