using ConsignmentGenie.Core.DTOs.Items;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface IItemImportService
{
    Task<ImportResultDto> ImportFromCsvAsync(Stream csvStream, Guid organizationId);
    byte[] GenerateTemplateAsync();
}