using ConsignmentGenie.Application.DTOs.Consignor;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Constants;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class ConsignorInvitationService : IConsignorInvitationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsignorInvitationService> _logger;
    private readonly INotificationService _notificationService;

    public ConsignorInvitationService(ConsignmentGenieContext context, IEmailService emailService, IConfiguration configuration, ILogger<ConsignorInvitationService> logger, INotificationService notificationService)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<ConsignorInvitationResultDto> CreateInvitationAsync(CreateConsignorInvitationDto request, Guid organizationId, Guid invitedById)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new ConsignorInvitationResultDto
                {
                    Success = false,
                    Message = "A user with this email already exists in the system."
                };
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.ConsignorInvitations
                .FirstOrDefaultAsync(pi => pi.Email.ToLower() == request.Email.ToLower()
                                          && pi.OrganizationId == organizationId
                                          && pi.Status == InvitationStatus.Pending);

            if (existingInvitation != null)
            {
                return new ConsignorInvitationResultDto
                {
                    Success = false,
                    Message = "A pending invitation already exists for this email address."
                };
            }

            // Create new invitation
            var invitation = new ConsignorInvitation
            {
                OrganizationId = organizationId,
                InvitedById = invitedById,
                Email = request.Email,
                Name = request.Name,
                Token = GenerateSecureToken(),
                Status = InvitationStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day expiration
            };

            _context.ConsignorInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Get organization and inviter details for email
            var organization = await _context.Organizations.FindAsync(organizationId);
            var inviter = await _context.Users.FindAsync(invitedById);

            // Generate invitation link
            var clientUrl = _configuration["ClientUrl"];

            _logger.LogInformation("Building consignor invitation link - ClientUrl: '{ClientUrl}'", clientUrl);

            var baseUrl = clientUrl ?? "http://localhost:4200";
            var inviteLink = $"{baseUrl}/register/consignor/invitation?token={invitation.Token}&storeCode={organization?.StoreCode}";

            _logger.LogInformation("Generated consignor invitation link: '{InviteLink}' for email: '{Email}'", inviteLink, invitation.Email);

            // Send invitation email
            var emailSent = await _emailService.SendConsignorInvitationAsync(
                invitation.Email,
                invitation.Name,
                organization?.Name ?? "ConsignmentGenie Shop",
                inviteLink,
                invitation.ExpiresAt.ToString("MMMM dd, yyyy"),
                request.PersonalMessage
            );

            var result = await GetInvitationDtoAsync(invitation.Id);

            return new ConsignorInvitationResultDto
            {
                Success = true,
                Message = "Invitation sent successfully.",
                Invitation = result
            };
        }
        catch (Exception ex)
        {
            return new ConsignorInvitationResultDto
            {
                Success = false,
                Message = $"Failed to create invitation: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<ConsignorInvitationDto>> GetPendingInvitationsAsync(Guid organizationId)
    {
        return await _context.ConsignorInvitations
            .Where(pi => pi.OrganizationId == organizationId && pi.Status == InvitationStatus.Pending)
            .Include(pi => pi.InvitedBy)
            .Select(pi => new ConsignorInvitationDto
            {
                Id = pi.Id,
                Name = pi.Name,
                Email = pi.Email,
                Status = pi.Status,
                ExpiresAt = pi.ExpiresAt,
                CreatedAt = pi.CreatedAt,
                InvitedByEmail = pi.InvitedBy.Email
            })
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync();
    }

    public async Task<ConsignorInvitationDto?> GetInvitationByTokenAsync(string token)
    {
        var invitation = await _context.ConsignorInvitations
            .Include(pi => pi.InvitedBy)
            .Include(pi => pi.Organization)
            .FirstOrDefaultAsync(pi => pi.Token == token);

        if (invitation == null)
            return null;

        return new ConsignorInvitationDto
        {
            Id = invitation.Id,
            Name = invitation.Name,
            Email = invitation.Email,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            CreatedAt = invitation.CreatedAt,
            InvitedByEmail = invitation.InvitedBy.Email
        };
    }

    public async Task<bool> CancelInvitationAsync(Guid invitationId, Guid organizationId)
    {
        var invitation = await _context.ConsignorInvitations
            .FirstOrDefaultAsync(pi => pi.Id == invitationId && pi.OrganizationId == organizationId);

        if (invitation == null || invitation.Status != InvitationStatus.Pending)
            return false;

        invitation.Status = InvitationStatus.Cancelled;
        invitation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResendInvitationAsync(Guid invitationId, Guid organizationId)
    {
        var invitation = await _context.ConsignorInvitations
            .Include(pi => pi.Organization)
            .Include(pi => pi.InvitedBy)
            .FirstOrDefaultAsync(pi => pi.Id == invitationId && pi.OrganizationId == organizationId);

        if (invitation == null || invitation.Status != InvitationStatus.Pending)
            return false;

        // Extend expiration
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        invitation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Generate invitation link and send email
        var clientUrl = _configuration["ClientUrl"];

        _logger.LogInformation("Building consignor resend invitation link - ClientUrl: '{ClientUrl}'", clientUrl);

        var baseUrl = clientUrl ?? "http://localhost:4200";
        var inviteLink = $"{baseUrl}/register/consignor/invitation?token={invitation.Token}&storeCode={invitation.Organization?.StoreCode}";

        _logger.LogInformation("Generated consignor resend invitation link: '{InviteLink}' for email: '{Email}'", inviteLink, invitation.Email);

        var emailSent = await _emailService.SendConsignorInvitationAsync(
            invitation.Email,
            invitation.Name,
            invitation.Organization?.Name ?? "ConsignmentGenie Shop",
            inviteLink,
            invitation.ExpiresAt.ToString("MMMM dd, yyyy"),
            null // No personal message for resends
        );

        return true;
    }

    public async Task<RegisterConsignorFromInvitationResponse> RegisterFromInvitationAsync(RegisterConsignorFromInvitationRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate invitation
            var invitation = await _context.ConsignorInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == request.InvitationToken &&
                                         i.Status == InvitationStatus.Pending &&
                                         i.ExpiresAt > DateTime.UtcNow);

            if (invitation == null)
            {
                return new RegisterConsignorFromInvitationResponse
                {
                    Success = false,
                    Message = "Invalid or expired invitation"
                };
            }

            // Check if email matches invitation
            if (!string.Equals(invitation.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                return new RegisterConsignorFromInvitationResponse
                {
                    Success = false,
                    Message = "Email address does not match invitation"
                };
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return new RegisterConsignorFromInvitationResponse
                {
                    Success = false,
                    Message = "A user with this email already exists"
                };
            }

            // Generate consignor number
            var consignorNumber = await GenerateConsignorNumberAsync(invitation.OrganizationId);

            // Create user account
            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Consignor,
                OrganizationId = invitation.OrganizationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create consignor record
            var consignor = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = invitation.OrganizationId,
                UserId = user.Id,
                ConsignorNumber = consignorNumber,
                Name = request.Name.Trim(),
                PreferredName = request.PreferredName?.Trim(),
                Email = request.Email,
                Phone = request.Phone,
                Status = invitation.Organization?.ApprovalMode == ApprovalMode.Auto
                    ? ConsignorStatus.Active
                    : ConsignorStatus.Pending,
                ApprovalStatus = invitation.Organization?.ApprovalMode == ApprovalMode.Auto
                    ? "Approved"
                    : "Pending",
                CommissionRate = (invitation.Organization?.DefaultSplitPercentage ?? 60.00m) / 100m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id
            };

            if (consignor.Status == ConsignorStatus.Active)
            {
                consignor.ApprovedAt = DateTime.UtcNow;
                consignor.ApprovedBy = user.Id; // Self-approved via auto-approval
            }

            _context.Consignors.Add(consignor);

            // Create agreement if required by organization
            await CreateConsignorAgreementIfRequiredAsync(consignor, invitation.Organization, request.Name, "", request.Email);

            // Mark invitation as accepted
            invitation.Status = InvitationStatus.Accepted;
            invitation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create notifications
            try
            {
                // Get the owner user ID for the notification
                var ownerUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.OrganizationId == invitation.OrganizationId && u.Role == UserRole.Owner);

                if (ownerUser != null)
                {
                    // Create notification for owner about new consignor
                    var ownerNotification = new CreateNotificationRequest
                    {
                        OrganizationId = invitation.OrganizationId,
                        FromUserId = user.Id, // From the consignor user
                        FromType = "consignor",
                        ToUserId = ownerUser.Id,
                        ToType = "owner",
                        Type = NotificationTypes.NEW_CONSIGNOR_REQUEST,
                        Title = "New Consignor Registered",
                        Message = $"{request.Name} has completed their registration and is ready to start consigning.",
                        ActionUrl = "/owner/consignors",
                        ReferenceType = "consignor",
                        ReferenceId = consignor.Id,
                        Payload = new
                        {
                            consignorId = consignor.Id,
                            consignorNumber = consignorNumber,
                            consignorName = request.Name,
                            email = request.Email,
                            phone = request.Phone,
                            status = consignor.Status.ToString()
                        }
                    };

                    await _notificationService.CreateAsync(ownerNotification);
                }

                // Create welcome notification for the new consignor
                var consignorNotification = new CreateNotificationRequest
                {
                    OrganizationId = invitation.OrganizationId,
                    FromUserId = null, // System notification
                    FromType = "system",
                    ToUserId = user.Id,
                    ToType = "consignor",
                    Type = NotificationTypes.CONSIGNOR_WELCOME,
                    Title = "Welcome to ConsignmentGenie!",
                    Message = $"Welcome to {invitation.Organization?.Name ?? "the shop"}! Your consignor account has been created successfully. You can now start uploading your items.",
                    ActionUrl = "/consignor/dashboard",
                    ReferenceType = "consignor",
                    ReferenceId = consignor.Id,
                    Payload = new
                    {
                        consignorId = consignor.Id,
                        consignorNumber = consignorNumber,
                        shopName = invitation.Organization?.Name,
                        commissionRate = consignor.CommissionRate
                    }
                };

                await _notificationService.CreateAsync(consignorNotification);

                _logger.LogInformation("[INVITATION] Created notifications for consignor registration: {Email}", request.Email);
            }
            catch (Exception notificationEx)
            {
                _logger.LogError(notificationEx, "[INVITATION] Failed to create notifications for consignor registration: {Email}", request.Email);
                // Continue with registration success even if notifications fail
            }

            await transaction.CommitAsync();

            // NOTE: Agreement handling is now done through upload workflow, not email notifications

            _logger.LogInformation("[INVITATION] Consignor registered successfully: {Email}, ConsignorNumber: {ConsignorNumber}",
                request.Email, consignorNumber);

            return new RegisterConsignorFromInvitationResponse
            {
                Success = true,
                Message = "Registration completed successfully",
                ConsignorId = consignor.Id,
                ConsignorNumber = consignorNumber
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[INVITATION] Failed to register consignor from invitation: {Email}", request.Email);

            return new RegisterConsignorFromInvitationResponse
            {
                Success = false,
                Message = "Registration failed. Please try again."
            };
        }
    }

    private async Task<string> GenerateConsignorNumberAsync(Guid organizationId)
    {
        var lastNumber = await _context.Consignors
            .Where(p => p.OrganizationId == organizationId)
            .OrderByDescending(p => p.ConsignorNumber)
            .Select(p => p.ConsignorNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length > 1 && int.TryParse(parts[1], out int num))
            {
                nextNumber = num + 1;
            }
        }

        return $"PRV-{nextNumber:D5}";
    }

    private async Task<ConsignorInvitationDto?> GetInvitationDtoAsync(Guid invitationId)
    {
        return await _context.ConsignorInvitations
            .Where(pi => pi.Id == invitationId)
            .Include(pi => pi.InvitedBy)
            .Select(pi => new ConsignorInvitationDto
            {
                Id = pi.Id,
                Name = pi.Name,
                Email = pi.Email,
                Status = pi.Status,
                ExpiresAt = pi.ExpiresAt,
                CreatedAt = pi.CreatedAt,
                InvitedByEmail = pi.InvitedBy.Email
            })
            .FirstOrDefaultAsync();
    }

    // OLD EMAIL AGREEMENT METHOD REMOVED - Agreement handling is now done through upload workflow

    private async Task CreateConsignorAgreementIfRequiredAsync(
        Consignor consignor,
        Organization organization,
        string firstName,
        string lastName,
        string email)
    {
        try
        {
            var fullName = $"{firstName} {lastName}".Trim();

            _logger.LogInformation("[AGREEMENT_CREATION] Checking if agreement creation is required for consignor {ConsignorId} in organization {OrganizationId}",
                consignor.Id, organization.Id);

            // Check if organization has agreement requirements
            if (string.IsNullOrEmpty(organization.AgreementMethod) || organization.AgreementMethod == "none")
            {
                _logger.LogInformation("[AGREEMENT_CREATION] No agreement required for organization {OrganizationId}, method: {AgreementMethod}",
                    organization.Id, organization.AgreementMethod ?? "null");
                return;
            }

            // Check if agreement template is available
            if (string.IsNullOrEmpty(organization.AgreementTemplateCloudinaryUrl))
            {
                _logger.LogWarning("[AGREEMENT_CREATION] Agreement method is {AgreementMethod} but no agreement template configured for organization {OrganizationId}",
                    organization.AgreementMethod, organization.Id);
                return;
            }

            // Get the agreement template text (currently hardcoded as placeholder)
            // TODO: In future, extract actual text from uploaded PDF/document file
            var templateText = $@"CONSIGNMENT AGREEMENT

This agreement is made between {organization.Name} and {{{{ConsignorName}}}}.

TERMS AND CONDITIONS

1. CONSIGNMENT PERIOD
Items will be displayed for sale for a period of 90 days from the date of acceptance.

2. COMMISSION STRUCTURE
{organization.Name} will retain 40% commission on all sales. {{{{ConsignorName}}}} will receive 60% of the final sale price.

3. ITEM CONDITIONS
All items must be in good condition, clean, and suitable for resale. Items not meeting quality standards may be returned.

4. PRICING
Items will be priced at fair market value as determined by {organization.Name}. Price reductions may occur during the consignment period.

5. PAYMENT TERMS
Payments will be made monthly for items sold in the previous month.

6. RETRIEVAL
Unsold items must be retrieved within 14 days of the consignment period ending.

Agreement Date: {{{{CurrentDate}}}}
Consignor: {{{{ConsignorName}}}}
Email: {{{{ConsignorEmail}}}}

By proceeding, {{{{ConsignorName}}}} agrees to all terms outlined above.";

            // Process agreement text with data swaps
            var processedText = templateText;

            // Replace organization placeholders
            processedText = processedText.Replace("{{OrganizationName}}", organization.Name);
            processedText = processedText.Replace("{{ShopName}}", organization.Name);

            // Replace consignor placeholders
            processedText = processedText.Replace("{{ConsignorName}}", fullName);
            processedText = processedText.Replace("{{ConsignorEmail}}", email);

            // Replace date placeholders
            processedText = processedText.Replace("{{CurrentDate}}", DateTime.UtcNow.ToString("MMMM dd, yyyy"));
            processedText = processedText.Replace("{{CurrentYear}}", DateTime.UtcNow.Year.ToString());

            // Determine status and method based on organization agreement method
            var agreementStatus = organization.AgreementMethod.ToLower() switch
            {
                "acknowledge" => "pending", // User needs to acknowledge
                "upload" => "pending",      // User needs to upload signed document
                _ => "pending"
            };

            var agreementMethod = organization.AgreementMethod.ToLower();

            // Create ConsignorAgreement record
            var consignorAgreement = new ConsignorAgreement
            {
                ConsignorId = consignor.Id,
                OrganizationId = organization.Id,
                Status = agreementStatus,
                Method = agreementMethod,
                Notes = processedText, // Store the processed agreement text in Notes for now
                CreatedAt = DateTime.UtcNow
            };

            _context.ConsignorAgreements.Add(consignorAgreement);

            _logger.LogInformation("[AGREEMENT_CREATION] Created ConsignorAgreement for consignor {ConsignorId}, method: {Method}, status: {Status}",
                consignor.Id, agreementMethod, agreementStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT_CREATION] Failed to create ConsignorAgreement for consignor {ConsignorId} in organization {OrganizationId}",
                consignor.Id, organization.Id);
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("/", "_").Replace("+", "-").Replace("=", "");
    }
}