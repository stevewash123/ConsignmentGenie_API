using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Models.Notifications;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsignmentGenie.Application.Services;

public class SuggestionService : ISuggestionService
{
    private readonly ConsignmentGenieContext _context;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SuggestionService> _logger;

    public SuggestionService(
        ConsignmentGenieContext context,
        INotificationService notificationService,
        IConfiguration configuration,
        ILogger<SuggestionService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ServiceResult<SuggestionDto>> CreateSuggestionAsync(
        CreateSuggestionRequest request,
        Guid userId,
        Guid organizationId)
    {
        try
        {
            // Get user information
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId);

            if (user == null)
            {
                return ServiceResult<SuggestionDto>.FailureResult("User not found");
            }

            // Create suggestion entity
            var suggestion = new Suggestion
            {
                OrganizationId = organizationId,
                UserId = userId,
                UserEmail = user.Email,
                UserName = user.Email.Split('@')[0], // Use email username part since BusinessName doesn't exist
                Type = request.Type,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.Suggestions.Add(suggestion);
            await _context.SaveChangesAsync();

            // Send notification using the new notification service (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    // Get the admin user ID (you can be the first admin or configure this)
                    var adminUserId = await GetAdminUserIdAsync();
                    if (adminUserId.HasValue)
                    {
                        var notificationRequest = new NotificationRequest(
                            adminUserId.Value,
                            NotificationType.SuggestionSubmitted,
                            new Dictionary<string, string>
                            {
                                { "SuggesterName", suggestion.UserName },
                                { "SuggesterEmail", suggestion.UserEmail },
                                { "SuggestionType", suggestion.Type.ToString() },
                                { "SuggestionTitle", GetSuggestionTitle(suggestion.Type) },
                                { "Message", suggestion.Message },
                                { "OrganizationName", user.Organization?.Name ?? "ConsignmentGenie" }
                            });

                        var success = await _notificationService.SendAsync(notificationRequest);

                        if (success)
                        {
                            suggestion.EmailSent = true;
                            suggestion.EmailSentAt = DateTime.UtcNow;
                            _context.Update(suggestion);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send suggestion notification for suggestion {SuggestionId}", suggestion.Id);
                }
            });

            var dto = new SuggestionDto
            {
                Id = suggestion.Id,
                Type = suggestion.Type,
                Message = suggestion.Message,
                UserEmail = suggestion.UserEmail,
                UserName = suggestion.UserName,
                IsProcessed = suggestion.IsProcessed,
                CreatedAt = suggestion.CreatedAt,
                ProcessedAt = suggestion.ProcessedAt
            };

            return ServiceResult<SuggestionDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating suggestion for user {UserId}", userId);
            return ServiceResult<SuggestionDto>.FailureResult("An error occurred while creating the suggestion");
        }
    }

    public async Task<PagedResult<SuggestionDto>> GetSuggestionsAsync(Guid organizationId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Suggestions
                .Where(s => s.OrganizationId == organizationId)
                .OrderByDescending(s => s.CreatedAt);

            var totalCount = await query.CountAsync();
            var suggestions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SuggestionDto
                {
                    Id = s.Id,
                    Type = s.Type,
                    Message = s.Message,
                    UserEmail = s.UserEmail,
                    UserName = s.UserName,
                    IsProcessed = s.IsProcessed,
                    CreatedAt = s.CreatedAt,
                    ProcessedAt = s.ProcessedAt
                })
                .ToListAsync();

            return new PagedResult<SuggestionDto>
            {
                Items = suggestions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving suggestions for organization {OrganizationId}", organizationId);
            return new PagedResult<SuggestionDto>
            {
                Items = new List<SuggestionDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public async Task<ServiceResult<bool>> MarkSuggestionProcessedAsync(Guid suggestionId, Guid organizationId, string? adminNotes = null)
    {
        try
        {
            var suggestion = await _context.Suggestions
                .FirstOrDefaultAsync(s => s.Id == suggestionId && s.OrganizationId == organizationId);

            if (suggestion == null)
            {
                return ServiceResult<bool>.FailureResult("Suggestion not found");
            }

            suggestion.IsProcessed = true;
            suggestion.ProcessedAt = DateTime.UtcNow;
            suggestion.AdminNotes = adminNotes;

            _context.Update(suggestion);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking suggestion {SuggestionId} as processed", suggestionId);
            return ServiceResult<bool>.FailureResult("An error occurred while updating the suggestion");
        }
    }

    private async Task<Guid?> GetAdminUserIdAsync()
    {
        try
        {
            // Try to get from configuration first
            var configuredAdminEmail = _configuration["AdminEmail"];
            if (!string.IsNullOrEmpty(configuredAdminEmail))
            {
                var adminUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == configuredAdminEmail);
                if (adminUser != null)
                {
                    return adminUser.Id;
                }
            }

            // Fallback: get the first owner user (there's no Admin role in this system)
            var firstAdmin = await _context.Users
                .Where(u => u.Role == UserRole.Owner)
                .OrderBy(u => u.CreatedAt)
                .FirstOrDefaultAsync();

            return firstAdmin?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin user ID");
            return null;
        }
    }

    private string GetSuggestionTitle(SuggestionType type)
    {
        return type switch
        {
            SuggestionType.FeatureRequest => "Feature Request",
            SuggestionType.BugReport => "Bug Report",
            SuggestionType.Improvement => "Improvement",
            SuggestionType.Integration => "Integration",
            SuggestionType.UserExperience => "User Experience",
            SuggestionType.Performance => "Performance",
            SuggestionType.Documentation => "Documentation",
            SuggestionType.Other => "Other",
            _ => "Suggestion"
        };
    }
}