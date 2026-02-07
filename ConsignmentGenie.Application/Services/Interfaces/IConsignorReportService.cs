using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IConsignorReportService
{
    Task<ServiceResult<ConsignorPerformanceReportDto>> GetConsignorPerformanceReportAsync(Guid organizationId, ConsignorPerformanceFilterDto filter);
}