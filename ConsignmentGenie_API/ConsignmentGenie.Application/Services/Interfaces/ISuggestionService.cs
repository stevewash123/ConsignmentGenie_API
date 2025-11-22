using ConsignmentGenie.Application.DTOs;

namespace ConsignmentGenie.Application.Services.Interfaces;

public interface ISuggestionService
{
    Task<ServiceResult<SuggestionDto>> CreateSuggestionAsync(CreateSuggestionRequest request, Guid userId, Guid organizationId);
    Task<PagedResult<SuggestionDto>> GetSuggestionsAsync(Guid organizationId, int page = 1, int pageSize = 20);
    Task<ServiceResult<bool>> MarkSuggestionProcessedAsync(Guid suggestionId, Guid organizationId, string? adminNotes = null);
}