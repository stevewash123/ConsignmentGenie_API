using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IProviderReportService
{
    Task<ServiceResult<ProviderPerformanceReportDto>> GetProviderPerformanceReportAsync(Guid organizationId, ProviderPerformanceFilterDto filter);
}