using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Reports;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IInventoryReportService
{
    Task<ServiceResult<InventoryAgingReportDto>> GetInventoryAgingReportAsync(Guid organizationId, InventoryAgingFilterDto filter);
    Task<ServiceResult<InventoryOverviewDto>> GetInventoryOverviewAsync(Guid organizationId);
}