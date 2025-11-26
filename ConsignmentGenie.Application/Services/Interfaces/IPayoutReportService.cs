using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IPayoutReportService
{
    Task<ServiceResult<PayoutSummaryReportDto>> GetPayoutSummaryReportAsync(Guid organizationId, PayoutSummaryFilterDto filter);
}