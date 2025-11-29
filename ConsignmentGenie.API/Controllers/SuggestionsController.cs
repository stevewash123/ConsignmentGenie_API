using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuggestionsController : ControllerBase
{
    private readonly ISuggestionService _suggestionService;

    public SuggestionsController(ISuggestionService suggestionService)
    {
        _suggestionService = suggestionService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SuggestionDto>>> CreateSuggestion([FromBody] CreateSuggestionRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(organizationIdClaim) ||
                !Guid.TryParse(userIdClaim, out var userId) ||
                !Guid.TryParse(organizationIdClaim, out var organizationId))
            {
                return BadRequest(ApiResponse<SuggestionDto>.ErrorResult("Invalid user authentication"));
            }

            var result = await _suggestionService.CreateSuggestionAsync(request, userId, organizationId);

            if (result.Success)
            {
                return Ok(ApiResponse<SuggestionDto>.SuccessResult(result.Data!));
            }

            return BadRequest(ApiResponse<SuggestionDto>.ErrorResult(result.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<SuggestionDto>.ErrorResult("An error occurred while creating the suggestion"));
        }
    }

    [HttpGet]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<SuggestionDto>>>> GetSuggestions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;

            if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
            {
                return BadRequest(ApiResponse<PagedResult<SuggestionDto>>.ErrorResult("Invalid organization authentication"));
            }

            var result = await _suggestionService.GetSuggestionsAsync(organizationId, page, pageSize);

            return Ok(ApiResponse<PagedResult<SuggestionDto>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<PagedResult<SuggestionDto>>.ErrorResult("An error occurred while retrieving suggestions"));
        }
    }

    [HttpPut("{suggestionId}/process")]
    [Authorize(Roles = "Owner,Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkSuggestionProcessed(Guid suggestionId, [FromBody] MarkSuggestionProcessedRequest request)
    {
        try
        {
            var organizationIdClaim = User.FindFirst("OrganizationId")?.Value;

            if (string.IsNullOrEmpty(organizationIdClaim) || !Guid.TryParse(organizationIdClaim, out var organizationId))
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Invalid organization authentication"));
            }

            var result = await _suggestionService.MarkSuggestionProcessedAsync(suggestionId, organizationId, request.AdminNotes);

            if (result.Success)
            {
                return Ok(ApiResponse<bool>.SuccessResult(result.Data));
            }

            return BadRequest(ApiResponse<bool>.ErrorResult(result.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<bool>.ErrorResult("An error occurred while processing the suggestion"));
        }
    }

    [HttpGet("types")]
    public ActionResult<ApiResponse<List<LookupDto>>> GetSuggestionTypes()
    {
        var suggestionTypes = Enum.GetValues<SuggestionType>()
            .Select(st => new LookupDto
            {
                Value = ((int)st).ToString(),
                Label = st.ToString()
            })
            .ToList();

        return Ok(ApiResponse<List<LookupDto>>.SuccessResult(suggestionTypes));
    }
}

public class MarkSuggestionProcessedRequest
{
    public string? AdminNotes { get; set; }
}