using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace ConsignmentGenie.Application.Services;

public class OwnerInvitationService : IOwnerInvitationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OwnerInvitationService> _logger;
    private readonly IAuthService _authService;

    public OwnerInvitationService(
        ConsignmentGenieContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<OwnerInvitationService> logger,
        IAuthService authService)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _authService = authService;
    }

    public async Task<ServiceResult<OwnerInvitationDetailDto>> CreateInvitationAsync(CreateOwnerInvitationRequest request, Guid invitedById)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return ServiceResult<OwnerInvitationDetailDto>.FailureResult("A user with this email already exists in the system.");
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.OwnerInvitations
                .FirstOrDefaultAsync(oi => oi.Email.ToLower() == request.Email.ToLower()
                                          && oi.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                return ServiceResult<OwnerInvitationDetailDto>.FailureResult("A pending invitation already exists for this email address.");
            }

            // Create new invitation
            var invitation = new OwnerInvitation
            {
                InvitedById = invitedById,
                Email = request.Email,
                Name = request.Name,
                Token = GenerateSecureToken(),
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day expiration
            };

            _context.OwnerInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Get inviter details for email
            var inviter = await _context.Users.FindAsync(invitedById);

            // Generate invitation link
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
            var inviteLink = $"{baseUrl}/owner/register?token={invitation.Token}";

            // Send invitation email
            var emailSent = await SendInvitationEmailAsync(invitation, inviteLink);

            var result = await GetInvitationDetailAsync(invitation.Id);

            return ServiceResult<OwnerInvitationDetailDto>.SuccessResult(result!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating owner invitation for {Email}", request.Email);
            return ServiceResult<OwnerInvitationDetailDto>.FailureResult("An error occurred while creating the invitation.");
        }
    }

    public async Task<ConsignmentGenie.Core.DTOs.PagedResult<OwnerInvitationListDto>> GetInvitationsAsync(OwnerInvitationQueryParams queryParams)
    {
        var query = _context.OwnerInvitations
            .Include(oi => oi.InvitedBy)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(queryParams.Search))
        {
            var searchTerm = queryParams.Search.ToLower();
            query = query.Where(oi => oi.Name.ToLower().Contains(searchTerm) ||
                                     oi.Email.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(queryParams.Status))
        {
            if (Enum.TryParse<InvitationStatus>(queryParams.Status, true, out var status))
            {
                query = query.Where(oi => oi.Status == status);
            }
        }

        // Apply sorting
        switch (queryParams.SortBy?.ToLower())
        {
            case "name":
                query = queryParams.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(oi => oi.Name)
                    : query.OrderBy(oi => oi.Name);
                break;
            case "email":
                query = queryParams.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(oi => oi.Email)
                    : query.OrderBy(oi => oi.Email);
                break;
            case "expiresat":
                query = queryParams.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(oi => oi.ExpiresAt)
                    : query.OrderBy(oi => oi.ExpiresAt);
                break;
            default: // CreatedAt
                query = queryParams.SortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(oi => oi.CreatedAt)
                    : query.OrderBy(oi => oi.CreatedAt);
                break;
        }

        var totalCount = await query.CountAsync();
        var skip = (queryParams.Page - 1) * queryParams.PageSize;

        var invitations = await query
            .Skip(skip)
            .Take(queryParams.PageSize)
            .Select(oi => new OwnerInvitationListDto
            {
                Id = oi.Id,
                Name = oi.Name,
                Email = oi.Email,
                Status = oi.Status.ToString(),
                InvitedByName = oi.InvitedBy.Email ?? "Unknown",
                CreatedAt = oi.CreatedAt,
                ExpiresAt = oi.ExpiresAt,
                IsExpired = oi.ExpiresAt < DateTime.UtcNow && oi.Status == InvitationStatus.Pending
            })
            .ToListAsync();

        return new ConsignmentGenie.Core.DTOs.PagedResult<OwnerInvitationListDto>(
            invitations,
            totalCount,
            queryParams.Page,
            queryParams.PageSize
        );
    }

    public async Task<OwnerInvitationDetailDto?> GetInvitationByIdAsync(Guid invitationId)
    {
        return await GetInvitationDetailAsync(invitationId);
    }

    public async Task<ValidateInvitationResponse> ValidateTokenAsync(string token)
    {
        try
        {
            var invitation = await _context.OwnerInvitations
                .FirstOrDefaultAsync(oi => oi.Token == token);

            if (invitation == null)
            {
                return new ValidateInvitationResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid invitation token."
                };
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return new ValidateInvitationResponse
                {
                    IsValid = false,
                    ErrorMessage = "This invitation is no longer valid."
                };
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                // Mark as expired
                invitation.Status = InvitationStatus.Expired;
                await _context.SaveChangesAsync();

                return new ValidateInvitationResponse
                {
                    IsValid = false,
                    ErrorMessage = "This invitation has expired."
                };
            }

            return new ValidateInvitationResponse
            {
                IsValid = true,
                Name = invitation.Name,
                Email = invitation.Email,
                ExpiresAt = invitation.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token {Token}", token);
            return new ValidateInvitationResponse
            {
                IsValid = false,
                ErrorMessage = "An error occurred while validating the invitation."
            };
        }
    }

    public async Task<ServiceResult<OwnerRegistrationResponse>> ProcessRegistrationAsync(OwnerRegistrationRequest request)
    {
        try
        {
            _logger.LogError("FLOW-3: Service started ProcessRegistrationAsync for Email={Email}, Token={TokenPrefix}",
                request.Email, request.Token?.Substring(0, Math.Min(8, request.Token.Length)));

            // Validate invitation token
            var invitation = await _context.OwnerInvitations
                .FirstOrDefaultAsync(oi => oi.Token == request.Token);

            _logger.LogError("FLOW-4: Token validation - Found invitation: {Found}",
                invitation != null);

            if (invitation == null || invitation.Status != InvitationStatus.Pending)
            {
                return ServiceResult<OwnerRegistrationResponse>.FailureResult("Invalid or expired invitation.");
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _context.SaveChangesAsync();
                return ServiceResult<OwnerRegistrationResponse>.FailureResult("This invitation has expired.");
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return ServiceResult<OwnerRegistrationResponse>.FailureResult("A user with this email already exists.");
            }

            // Check if subdomain is available
            var existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Subdomain.ToLower() == request.Subdomain.ToLower());

            if (existingOrg != null)
            {
                return ServiceResult<OwnerRegistrationResponse>.FailureResult("This subdomain is already taken.");
            }

            _logger.LogError("FLOW-5: Validation passed - Starting organization creation. Email={Email}, ShopName={ShopName}, Subdomain={Subdomain}",
                request.Email, request.ShopName, request.Subdomain ?? "NULL");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create organization
                var organization = new Organization
                {
                    Name = request.ShopName,
                    VerticalType = VerticalType.Consignment,
                    SubscriptionStatus = SubscriptionStatus.Trial,
                    SubscriptionTier = SubscriptionTier.Basic,
                    Subdomain = request.Subdomain.ToLower(),
                    Slug = request.Subdomain.ToLower(),
                    Status = "active",
                    SetupStep = 1
                };

                _logger.LogError("FLOW-6: About to save organization: Name={Name}, Subdomain={Subdomain}, Slug={Slug}",
                    organization.Name, organization.Subdomain ?? "NULL", organization.Slug ?? "NULL");

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync();

                _logger.LogError("FLOW-7: Organization saved with ID: {OrganizationId}", organization.Id);

                // Reload from database to verify it was saved correctly
                var savedOrganization = await _context.Organizations.FindAsync(organization.Id);
                _logger.LogError("FLOW-8: Reloaded organization: Id={Id}, Name={Name}, Subdomain={Subdomain}, Slug={Slug}",
                    savedOrganization?.Id, savedOrganization?.Name, savedOrganization?.Subdomain ?? "NULL", savedOrganization?.Slug ?? "NULL");

                // Create user
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = hashedPassword,
                    Role = UserRole.Owner,
                    OrganizationId = organization.Id,
                    FirstName = request.Name.Split(' ')[0],
                    LastName = request.Name.Contains(' ') ? request.Name.Substring(request.Name.IndexOf(' ') + 1) : ""
                };

                _logger.LogError("FLOW-9: About to save user: Email={Email}, OrganizationId={OrganizationId}",
                    user.Email, organization.Id);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogError("FLOW-10: User saved with ID: {UserId}", user.Id);

                // Mark invitation as accepted
                invitation.Status = InvitationStatus.Accepted;
                await _context.SaveChangesAsync();

                _logger.LogError("FLOW-11: Invitation marked as accepted, committing transaction");

                await transaction.CommitAsync();

                _logger.LogError("FLOW-12: Transaction committed successfully");

                // Send welcome email
                await _emailService.SendWelcomeEmailAsync(user.Email, organization.Name);

                // Generate JWT token for the new user to auto-login
                string? jwtToken = null;
                try
                {
                    var loginRequest = new LoginRequest
                    {
                        Email = user.Email,
                        Password = request.Password
                    };

                    _logger.LogError("FLOW-13: About to call AuthService.LoginAsync for user: {Email}", user.Email);
                    var loginResponse = await _authService.LoginAsync(loginRequest);
                    jwtToken = loginResponse?.Token;
                    _logger.LogError("FLOW-14: LoginAsync completed. HasToken={HasToken}", jwtToken != null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FLOW-15: JWT token generation failed for user {UserId}: {Error}", user.Id, ex.Message);
                }

                var redirectUrl = $"{_configuration["App:BaseUrl"] ?? "http://localhost:4200"}/owner/dashboard";

                _logger.LogError("FLOW-16: Creating response - UserId={UserId}, OrganizationId={OrganizationId}, HasToken={HasToken}, RedirectUrl={RedirectUrl}",
                    user.Id, organization.Id, jwtToken != null, redirectUrl);

                var response = new OwnerRegistrationResponse
                {
                    Success = true,
                    UserId = user.Id,
                    OrganizationId = organization.Id,
                    Token = jwtToken,
                    RedirectUrl = redirectUrl
                };

                _logger.LogError("FLOW-17: Returning success response with Token length: {TokenLength}",
                    jwtToken?.Length ?? 0);

                return ServiceResult<OwnerRegistrationResponse>.SuccessResult(response);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing owner registration for token {Token}", request.Token);
            return ServiceResult<OwnerRegistrationResponse>.FailureResult("An error occurred while processing the registration.");
        }
    }

    public async Task<ServiceResult<bool>> CancelInvitationAsync(Guid invitationId)
    {
        try
        {
            var invitation = await _context.OwnerInvitations.FindAsync(invitationId);

            if (invitation == null)
            {
                return ServiceResult<bool>.FailureResult("Invitation not found.");
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return ServiceResult<bool>.FailureResult("Only pending invitations can be cancelled.");
            }

            invitation.Status = InvitationStatus.Cancelled;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invitation {InvitationId}", invitationId);
            return ServiceResult<bool>.FailureResult("An error occurred while cancelling the invitation.");
        }
    }

    public async Task<ServiceResult<bool>> ResendInvitationAsync(Guid invitationId)
    {
        try
        {
            var invitation = await _context.OwnerInvitations.FindAsync(invitationId);

            if (invitation == null)
            {
                return ServiceResult<bool>.FailureResult("Invitation not found.");
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                return ServiceResult<bool>.FailureResult("Only pending invitations can be resent.");
            }

            // Extend expiration
            invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            // Resend email
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:4200";
            var inviteLink = $"{baseUrl}/owner/register?token={invitation.Token}";

            await SendInvitationEmailAsync(invitation, inviteLink);

            return ServiceResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation {InvitationId}", invitationId);
            return ServiceResult<bool>.FailureResult("An error occurred while resending the invitation.");
        }
    }

    public async Task<OwnerInvitationMetricsDto> GetMetricsAsync()
    {
        var total = await _context.OwnerInvitations.CountAsync();
        var pending = await _context.OwnerInvitations.CountAsync(oi => oi.Status == InvitationStatus.Pending);
        var accepted = await _context.OwnerInvitations.CountAsync(oi => oi.Status == InvitationStatus.Accepted);
        var expired = await _context.OwnerInvitations.CountAsync(oi => oi.Status == InvitationStatus.Expired);
        var cancelled = await _context.OwnerInvitations.CountAsync(oi => oi.Status == InvitationStatus.Cancelled);

        var thisMonth = DateTime.UtcNow.AddDays(-30);
        var thisMonthCount = await _context.OwnerInvitations.CountAsync(oi => oi.CreatedAt >= thisMonth);

        var acceptanceRate = total > 0 ? (decimal)accepted / total * 100 : 0;

        return new OwnerInvitationMetricsDto
        {
            TotalInvitations = total,
            PendingInvitations = pending,
            AcceptedInvitations = accepted,
            ExpiredInvitations = expired,
            CancelledInvitations = cancelled,
            InvitationsThisMonth = thisMonthCount,
            AcceptanceRate = Math.Round(acceptanceRate, 1)
        };
    }

    private async Task<OwnerInvitationDetailDto?> GetInvitationDetailAsync(Guid invitationId)
    {
        return await _context.OwnerInvitations
            .Where(oi => oi.Id == invitationId)
            .Include(oi => oi.InvitedBy)
            .Select(oi => new OwnerInvitationDetailDto
            {
                Id = oi.Id,
                Name = oi.Name,
                Email = oi.Email,
                Token = oi.Token,
                Status = oi.Status.ToString(),
                InvitedByName = oi.InvitedBy.Email ?? "Unknown",
                InvitedByEmail = oi.InvitedBy.Email ?? "Unknown",
                CreatedAt = oi.CreatedAt,
                ExpiresAt = oi.ExpiresAt,
                IsExpired = oi.ExpiresAt < DateTime.UtcNow && oi.Status == InvitationStatus.Pending,
                InvitationUrl = $"{_configuration["App:BaseUrl"] ?? "http://localhost:4200"}/owner/register?token={oi.Token}"
            })
            .FirstOrDefaultAsync();
    }

    private async Task<bool> SendInvitationEmailAsync(OwnerInvitation invitation, string inviteLink)
    {
        var subject = "You're invited to join ConsignmentGenie as a Shop Owner";
        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Shop Owner Invitation</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">You're Invited!</h1>
        <p style=""margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;"">Join ConsignmentGenie as a Shop Owner</p>
    </div>

    <div style=""background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; border: 1px solid #ddd;"">
        <h2 style=""color: #667eea; margin-top: 0;"">Hello {invitation.Name}!</h2>

        <p>You've been invited to join ConsignmentGenie as a shop owner and start managing your own consignment business.</p>

        <p>With ConsignmentGenie, you can:</p>

        <div style=""background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea;"">
            <ul style=""margin: 0; padding-left: 20px;"">
                <li>Manage providers and inventory</li>
                <li>Track sales and transactions</li>
                <li>Generate automated payout reports</li>
                <li>Accept online orders</li>
                <li>Export data for accounting</li>
            </ul>
        </div>

        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""{inviteLink}""
               style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                      color: white;
                      padding: 15px 30px;
                      text-decoration: none;
                      border-radius: 25px;
                      font-weight: bold;
                      font-size: 16px;
                      display: inline-block;
                      box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
                      transition: all 0.3s ease;"">
                Accept Invitation & Get Started
            </a>
        </div>

        <div style=""background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 5px; padding: 15px; margin: 20px 0; color: #856404;"">
            <strong>⏰ Important:</strong> This invitation expires on {invitation.ExpiresAt:MMMM dd, yyyy}. Please register before then to secure your access.
        </div>

        <hr style=""border: none; border-top: 1px solid #ddd; margin: 30px 0;"">

        <div style=""margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #999; text-align: center;"">
            <p>If you have any questions, please contact support at support@microsaasbuilders.com</p>
            <p>This email was sent by ConsignmentGenie.</p>
        </div>
    </div>
</body>
</html>";

        return await _emailService.SendSimpleEmailAsync(invitation.Email, subject, htmlContent);
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}