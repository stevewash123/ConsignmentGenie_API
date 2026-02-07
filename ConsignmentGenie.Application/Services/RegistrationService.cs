using ConsignmentGenie.Application.Helpers;
using ConsignmentGenie.Core.Constants;
using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Extensions;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using System.Text.Json;

namespace ConsignmentGenie.Application.Services;

public class RegistrationService : IRegistrationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IStoreCodeService _storeCodeService;
    private readonly IAuthService _authService;
    private readonly IConsignorNotificationService _notificationService;
    private readonly INotificationService _unifiedNotificationService;
    private readonly ISlugService _slugService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        ConsignmentGenieContext context,
        IEmailService emailService,
        IStoreCodeService storeCodeService,
        IAuthService authService,
        IConsignorNotificationService notificationService,
        INotificationService unifiedNotificationService,
        ISlugService slugService,
        ISubscriptionService subscriptionService,
        ILogger<RegistrationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _storeCodeService = storeCodeService;
        _authService = authService;
        _notificationService = notificationService;
        _unifiedNotificationService = unifiedNotificationService;
        _slugService = slugService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<StoreCodeValidationDto> ValidateStoreCodeAsync(string code)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.StoreCode == code && o.StoreCodeEnabled);

        if (organization == null)
        {
            return new StoreCodeValidationDto
            {
                IsValid = false,
                ErrorMessage = "Invalid or disabled store code"
            };
        }

        return new StoreCodeValidationDto
        {
            IsValid = true,
            ShopName = organization.ShopName ?? organization.Name
        };
    }

    public async Task<RegistrationResultDto> RegisterOwnerAsync(RegisterOwnerRequest request)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "An account with this email already exists.",
                    Errors = new List<string> { "Email already in use" }
                };
            }

            // Generate slug from shop name + zip code if subdomain is not provided
            var slug = !string.IsNullOrEmpty(request.Subdomain)
                ? request.Subdomain
                : _slugService.GenerateSlug($"{request.ShopName}-{request.ZipCode}");

            // Ensure slug is unique by checking for existing organizations
            var baseSlug = slug;
            var counter = 1;
            while (await _context.Organizations.AnyAsync(o => o.Subdomain == slug || o.Slug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            // Create organization with retry logic for store code collisions
            const int maxRetries = 5;
            Organization organization = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    organization = new Organization
                    {
                        Name = request.ShopName,
                        ShopName = request.ShopName,
                        Subdomain = request.Subdomain, // May be null if not provided
                        Slug = slug, // Use generated slug
                        StoreCode = _storeCodeService.GenerateStoreCode(),
                        StoreCodeEnabled = true,
                        CachedSubscriptionStatus = "trialing" // Initial status before Stripe subscription
                    };

                    _context.Organizations.Add(organization);
                    await _context.SaveChangesAsync();
                    break; // Success, exit retry loop
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
                {
                    // Remove the failed organization from tracking
                    if (organization != null)
                    {
                        _context.Organizations.Remove(organization);
                    }

                    // Collision occurred, retry with new code
                    if (attempt == maxRetries - 1)
                        throw new Exception("Failed to generate unique store code after multiple attempts");
                }
            }

            // Create user (auto-approved for low friction signup)
            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Phone = request.Phone,
                Role = UserRole.Owner,
                ApprovalStatus = ApprovalStatus.Approved, // Auto-approve for immediate access
                ApprovedAt = DateTime.UtcNow,
                OrganizationId = organization.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Stripe customer and trial subscription
            try
            {
                var (customerId, subscriptionId) = await _subscriptionService.CreateTrialSubscriptionAsync(
                    organization.Id,
                    request.Email,
                    request.ShopName
                );

                // Update organization with Stripe IDs
                organization.StripeCustomerId = customerId;
                organization.StripeSubscriptionId = subscriptionId;
                organization.CachedSubscriptionStatus = "trialing";
                await _context.SaveChangesAsync();

                _logger.LogInformation("[OWNER_REGISTRATION] Created Stripe trial subscription for {Email}: Customer={CustomerId}, Subscription={SubscriptionId}",
                    request.Email, customerId, subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OWNER_REGISTRATION] Failed to create Stripe subscription for {Email}", request.Email);
                // Continue with registration - subscription can be created later
            }

            // Create shop consignor for shop-owned inventory
            var shopConsignor = new Consignor
            {
                OrganizationId = organization.Id,
                UserId = null, // No user association for shop consignor
                ConsignorNumber = "SHOP-001", // Special number for shop consignor
                FirstName = organization.ShopName ?? organization.Name,
                LastName = "(Shop)",
                Email = request.Email, // Use shop owner's email
                Phone = request.Phone,
                CommissionRate = 1.0000m, // Shop keeps 100% of shop-owned item sales
                Status = ConsignorStatus.Active,
                IsShopOwned = true, // Mark as shop-owned consignor
                CreatedBy = user.Id,
                UpdatedBy = user.Id
            };

            _context.Consignors.Add(shopConsignor);
            await _context.SaveChangesAsync();

            // Send welcome email to owner using enhanced template
            await _emailService.SendWelcomeEmailAsync(
                request.Email,
                request.ShopName,
                request.FullName,
                organization.StoreCode);

            // Send welcome notification to owner
            try
            {
                var welcomeNotification = NotificationHelper.CreateWelcomeNotification(
                    user, "owner", organization.Id);

                await _notificationService.CreateNotificationAsync(welcomeNotification);
                _logger.LogInformation("[OWNER_REGISTRATION] Welcome notification sent to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OWNER_REGISTRATION] Failed to send welcome notification to {Email}", request.Email);
            }

            // Generate JWT token for immediate authentication
            var token = _authService.GenerateJwtToken(user.Id, user.Email, user.Role.ToStringValue(), user.OrganizationId);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            return new RegistrationResultDto
            {
                Success = true,
                Message = "Account created successfully! You can now log in and start setting up your shop.",
                Token = token,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                OrganizationId = user.OrganizationId,
                OrganizationName = organization.ShopName,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            return new RegistrationResultDto
            {
                Success = false,
                Message = "An error occurred during registration.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<RegistrationResultDto> RegisterConsignorAsync(RegisterConsignorRequest request)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Starting provider registration for email {Email} with store code {StoreCode}",
            request.Email, request.StoreCode);
        _logger.LogDebug("[PROVIDER_INVITATION] Consignor registration details: FullName={FullName}, Phone={Phone}, PreferredPayment={PreferredPaymentMethod}",
            request.FullName, request.Phone, request.PreferredPaymentMethod);

        try
        {
            // Validate store code
            _logger.LogDebug("[PROVIDER_INVITATION] Validating store code {StoreCode}", request.StoreCode);
            var validation = await ValidateStoreCodeAsync(request.StoreCode);
            if (!validation.IsValid)
            {
                _logger.LogWarning("[PROVIDER_INVITATION] Store code validation failed for {StoreCode}: {ErrorMessage}",
                    request.StoreCode, validation.ErrorMessage);
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = validation.ErrorMessage ?? "Invalid store code",
                    Errors = new List<string> { "Invalid store code" }
                };
            }
            _logger.LogInformation("[PROVIDER_INVITATION] Store code {StoreCode} validated successfully for shop {ShopName}",
                request.StoreCode, validation.ShopName);

            // Check if email already exists
            _logger.LogDebug("[PROVIDER_INVITATION] Checking if email {Email} already exists", request.Email);
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                _logger.LogWarning("[PROVIDER_INVITATION] Registration failed - email {Email} already exists", request.Email);
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "An account with this email already exists.",
                    Errors = new List<string> { "Email already in use" }
                };
            }

            // Get organization
            _logger.LogDebug("[PROVIDER_INVITATION] Retrieving organization for store code {StoreCode}", request.StoreCode);
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.StoreCode == request.StoreCode);

            if (organization == null)
            {
                _logger.LogError("[PROVIDER_INVITATION] Organization not found for store code {StoreCode}", request.StoreCode);
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Invalid store code",
                    Errors = new List<string> { "Store code not found" }
                };
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Found organization {OrganizationName} (ID: {OrganizationId}) for store code {StoreCode}, ApprovalMode: {ApprovalMode}",
                organization.Name, organization.Id, request.StoreCode, organization.ApprovalMode);

            // Create user
            _logger.LogInformation("[PROVIDER_INVITATION] Creating user account for {Email} with role Consignor, approval status will be {ApprovalStatus}",
                request.Email, organization.ApprovalMode == ApprovalMode.Auto ? "Approved" : "Pending");
            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Phone = request.Phone,
                Role = UserRole.Consignor,
                ApprovalStatus = organization.ApprovalMode == ApprovalMode.Auto ? ApprovalStatus.Approved : ApprovalStatus.Pending,
                OrganizationId = organization.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[PROVIDER_INVITATION] User {Email} created successfully with ID {UserId}", request.Email, user.Id);

            // If auto-approved, create Consignor record immediately
            if (organization.ApprovalMode == ApprovalMode.Auto)
            {
                _logger.LogInformation("[PROVIDER_INVITATION] Auto-approval enabled - creating Consignor record for {Email}", request.Email);
                var consignor = new Consignor
                {
                    OrganizationId = organization.Id,
                    UserId = user.Id,
                    FirstName = GetFirstName(request.FullName),
                    LastName = GetLastName(request.FullName),
                    Email = request.Email,
                    Phone = request.Phone,
                    PreferredPaymentMethod = request.PreferredPaymentMethod ?? "Check",
                    PaymentDetails = request.PaymentDetails,
                    Status = ConsignorStatus.Active,
                    ApprovalStatus = "Approved",
                    ApprovedBy = null // Auto-approved
                };

                _context.Consignors.Add(consignor);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[PROVIDER_INVITATION] Consignor record created for {Email} with ID {ConsignorId}", request.Email, consignor.Id);

                // Create ConsignorAgreement if organization has agreement requirements
                await CreateConsignorAgreementIfRequiredAsync(consignor, organization, request.FullName, request.Email);

                // Send welcome notification to the new consignor
                try
                {
                    var welcomeNotification = NotificationHelper.CreateWelcomeNotification(
                        user, "consignor", organization.Id);

                    await _notificationService.CreateNotificationAsync(welcomeNotification);
                    _logger.LogInformation("[PROVIDER_INVITATION] Welcome notification sent to {Email}", request.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PROVIDER_INVITATION] Failed to send welcome notification to {Email}", request.Email);
                }

                // NOTE: Agreement handling is now done through upload workflow, not email notifications
            }
            else
            {
                _logger.LogInformation("[PROVIDER_INVITATION] Manual approval required - Consignor record will be created after approval");
            }

            // Send confirmation email to provider
            _logger.LogInformation("[PROVIDER_INVITATION] Sending confirmation email to provider {Email}", request.Email);
            var consignorEmailBody = $@"
                <h2>Welcome to ConsignmentGenie</h2>
                <p>Hi {request.FullName},</p>
                <p>Thanks for registering with ConsignmentGenie!</p>
                <p>Your request to join {validation.ShopName} is {(organization.ApprovalMode == ApprovalMode.Auto ? "approved" : "pending approval")}.</p>
                {(organization.ApprovalMode == ApprovalMode.Auto
                    ? "<p>You can now log in to your Consignor Portal!</p>"
                    : "<p>The shop owner will review your request and you'll receive an email when your account is ready.</p>")}
                <p>Questions? Reply to this email.</p>
                <p>- The ConsignmentGenie Team</p>";

            var emailResult = await _emailService.SendSimpleEmailAsync(
                request.Email,
                organization.ApprovalMode == ApprovalMode.Auto ? "Account Approved - You're In! 🎉" : "Welcome to ConsignmentGenie - Account Pending",
                consignorEmailBody);
            _logger.LogInformation("[PROVIDER_INVITATION] Consignor confirmation email sent to {Email}: {EmailResult}", request.Email, emailResult);

            // Send notification to owner if not auto-approved
            if (organization.ApprovalMode == ApprovalMode.Manual)
            {
                _logger.LogInformation("[PROVIDER_INVITATION] Manual approval required - sending notification to shop owners");
                var ownerUsers = await _context.Users
                    .Where(u => u.OrganizationId == organization.Id && u.Role == UserRole.Owner)
                    .ToListAsync();

                _logger.LogDebug("[PROVIDER_INVITATION] Found {OwnerCount} owners to notify for organization {OrganizationId}",
                    ownerUsers.Count, organization.Id);

                foreach (var owner in ownerUsers)
                {
                    _logger.LogDebug("[PROVIDER_INVITATION] Sending owner notification to {OwnerEmail} for new provider request from {ProviderEmail}",
                        owner.Email, request.Email);
                    var ownerEmailBody = $@"
                        <h2>New Consignor Request</h2>
                        <p>Hi {validation.ShopName},</p>
                        <p>{request.FullName} has requested to join your shop as a provider.</p>
                        <p><strong>Name:</strong> {request.FullName}</p>
                        <p><strong>Email:</strong> {request.Email}</p>
                        <p><strong>Phone:</strong> {request.Phone ?? "Not provided"}</p>
                        <p><strong>Payment:</strong> {request.PreferredPaymentMethod ?? "Check"} ({request.PaymentDetails ?? "No details"})</p>
                        <p>Log in to review and approve this request.</p>
                        <p>- ConsignmentGenie</p>";

                    var ownerEmailResult = await _emailService.SendSimpleEmailAsync(
                        owner.Email,
                        $"New Consignor Request - {request.FullName}",
                        ownerEmailBody);
                    _logger.LogInformation("[PROVIDER_INVITATION] Owner notification sent to {OwnerEmail}: {EmailResult}",
                        owner.Email, ownerEmailResult);

                    // Send notification to owner about new consignor request
                    try
                    {
                        var newConsignorNotification = NotificationHelper.CreateNewConsignorPendingRequestNotification(
                            user, owner, organization.Id);

                        await _notificationService.CreateNotificationAsync(newConsignorNotification);
                        _logger.LogInformation("[PROVIDER_INVITATION] New consignor notification sent to owner {OwnerEmail}", owner.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[PROVIDER_INVITATION] Failed to send new consignor notification to owner {OwnerEmail}", owner.Email);
                    }
                }
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Consignor registration completed successfully for {Email} - ApprovalMode: {ApprovalMode}",
                request.Email, organization.ApprovalMode);

            return new RegistrationResultDto
            {
                Success = true,
                Message = organization.ApprovalMode == ApprovalMode.Auto
                    ? "Account created and approved! You can now log in."
                    : "Account created successfully. You'll receive an email when approved."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROVIDER_INVITATION] Consignor registration failed for {Email}", request.Email);
            return new RegistrationResultDto
            {
                Success = false,
                Message = "An error occurred during registration.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<List<PendingApprovalDto>> GetPendingConsignorsAsync(Guid organizationId)
    {
        var pendingUsers = await _context.Users
            .Where(u => u.OrganizationId == organizationId
                     && u.Role == UserRole.Consignor
                     && u.ApprovalStatus == ApprovalStatus.Pending)
            .Select(u => new PendingApprovalDto
            {
                UserId = u.Id,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email,
                Phone = u.Phone,
                // These would come from the registration request, but we need to store them on User entity
                // For now, leaving null - could be enhanced to store in User table or separate table
                PreferredPaymentMethod = null,
                PaymentDetails = null,
                RequestedAt = u.CreatedAt
            })
            .OrderBy(u => u.RequestedAt)
            .ToListAsync();

        return pendingUsers;
    }

    public async Task<int> GetPendingApprovalCountAsync(Guid organizationId)
    {
        return await _context.Users
            .CountAsync(u => u.OrganizationId == organizationId
                          && u.Role == UserRole.Consignor
                          && u.ApprovalStatus == ApprovalStatus.Pending);
    }

    public async Task ApproveUserAsync(Guid userId, Guid approvedByUserId)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Starting user approval process for user {UserId} by approver {ApprovedByUserId}",
            userId, approvedByUserId);

        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        // Load user with organization for business logic
        _logger.LogDebug("[PROVIDER_INVITATION] Loading user {UserId} with organization details", userId);
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogError("[PROVIDER_INVITATION] User {UserId} not found for approval", userId);
            throw new ArgumentException("User not found");
        }

        _logger.LogInformation("[PROVIDER_INVITATION] Found user {Email} with role {Role} in organization {OrganizationName} (ID: {OrganizationId}), current status: {ApprovalStatus}",
            user.Email, user.Role, user.Organization?.Name, user.OrganizationId, user.ApprovalStatus);

        if (user.ApprovalStatus != ApprovalStatus.Pending)
        {
            _logger.LogWarning("[PROVIDER_INVITATION] User {Email} is not pending approval - current status: {ApprovalStatus}",
                user.Email, user.ApprovalStatus);
            throw new InvalidOperationException("User is not pending approval");
        }

        // Update user status
        _logger.LogInformation("[PROVIDER_INVITATION] Approving user {Email} with role {Role}", user.Email, user.Role);
        user.ApprovalStatus = ApprovalStatus.Approved;
        user.ApprovedBy = approvedByUserId;
        user.ApprovedAt = DateTime.UtcNow;

        // 🏗️ AGGREGATE ROOT PATTERN: If provider, create provider record
        if (user.Role == UserRole.Consignor)
        {
            _logger.LogInformation("[PROVIDER_INVITATION] Creating Consignor record for approved user {Email}", user.Email);
            var consignorNumber = await GenerateProviderNumberAsync(user.OrganizationId);
            var consignor = new Consignor
            {
                OrganizationId = user.OrganizationId,
                UserId = user.Id,
                FirstName = GetFirstName(user.FullName),
                LastName = GetLastName(user.FullName),
                Email = user.Email,
                Phone = user.Phone,
                PreferredPaymentMethod = "Check", // Default, can be updated later
                Status = ConsignorStatus.Active,
                ApprovalStatus = "Approved",
                ApprovedBy = approvedByUserId,
                ConsignorNumber = consignorNumber
            };

            _context.Consignors.Add(consignor);
            _logger.LogInformation("[PROVIDER_INVITATION] Consignor record will be created with number {ConsignorNumber} for {Email}",
                consignorNumber, user.Email);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("[PROVIDER_INVITATION] User approval completed successfully for {Email} with role {Role}",
            user.Email, user.Role);

        // Send approval email
        _logger.LogInformation("[PROVIDER_INVITATION] Sending approval notification email to {Email} for role {Role}",
            user.Email, user.Role);
        var emailBody = user.Role == UserRole.Consignor
            ? $@"
                <h2>Account Approved - You're In! 🎉</h2>
                <p>Hi {user.FullName},</p>
                <p>Great news! Your account has been approved by {user.Organization.ShopName}.</p>
                <p>You can now log in to your Consignor Portal to:</p>
                <ul>
                    <li>View your consigned items</li>
                    <li>Track sales</li>
                    <li>See your earnings and payout history</li>
                </ul>
                <p>Welcome aboard!</p>
                <p>- The ConsignmentGenie Team</p>"
            : $@"
                <h2>Your Shop is Ready! 🎉</h2>
                <p>Hi {user.FullName},</p>
                <p>Great news! {user.Organization.ShopName} has been approved and is ready to go.</p>
                <p>Your store code for providers is: <strong>{user.Organization.StoreCode}</strong></p>
                <p>Share this with consigners so they can register and join your shop.</p>
                <p>Welcome to ConsignmentGenie!</p>
                <p>- The ConsignmentGenie Team</p>";

        var emailResult = await _emailService.SendSimpleEmailAsync(
            user.Email,
            user.Role == UserRole.Consignor ? "Account Approved - You're In! 🎉" : "Your Shop is Ready! 🎉",
            emailBody);
        _logger.LogInformation("[PROVIDER_INVITATION] Approval notification email sent to {Email}: {EmailResult}",
            user.Email, emailResult);
    }

    public async Task RejectUserAsync(Guid userId, Guid rejectedByUserId, string? reason)
    {
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new ArgumentException("User not found");

        if (user.ApprovalStatus != ApprovalStatus.Pending)
            throw new InvalidOperationException("User is not pending approval");

        // Update user status
        user.ApprovalStatus = ApprovalStatus.Rejected;
        user.RejectedReason = reason;

        await _context.SaveChangesAsync();

        // Send rejection email
        var emailBody = $@"
            <h2>Account Request Update</h2>
            <p>Hi {user.FullName},</p>
            <p>Unfortunately, your request to join {user.Organization.ShopName} was not approved at this time.</p>
            {(!string.IsNullOrEmpty(reason) ? $"<p><strong>Reason:</strong> {reason}</p>" : "")}
            <p>If you have questions, please contact the shop directly.</p>
            <p>- The ConsignmentGenie Team</p>";

        await _emailService.SendSimpleEmailAsync(
            user.Email,
            "Account Request Update",
            emailBody);
    }

    public async Task<List<PendingOwnerDto>> GetPendingOwnersAsync()
    {
        var pendingOwners = await _context.Users
            .Include(u => u.Organization)
            .Where(u => u.Role == UserRole.Owner && u.ApprovalStatus == ApprovalStatus.Pending)
            .Select(u => new PendingOwnerDto
            {
                UserId = u.Id,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email,
                Phone = u.Phone,
                ShopName = u.Organization.ShopName ?? u.Organization.Name,
                RequestedAt = u.CreatedAt
            })
            .OrderBy(u => u.RequestedAt)
            .ToListAsync();

        return pendingOwners;
    }

    public async Task ApproveOwnerAsync(Guid userId, Guid approvedByUserId)
    {
        // Load user with organization
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            throw new ArgumentException("User not found");
        
        // ✅ FIX BUG 1: Validate user is an owner
        if (user.Role != UserRole.Owner)
            throw new InvalidOperationException("User is not an owner");
        
        if (user.ApprovalStatus != ApprovalStatus.Pending)
            throw new InvalidOperationException("User is not pending approval");
        
        // Update user status
        user.ApprovalStatus = ApprovalStatus.Approved;
        user.ApprovedBy = approvedByUserId;
        user.ApprovedAt = DateTime.UtcNow;
        
        // ✅ FIX BUG 2: Generate store code for owner's organization
        if (user.Organization != null && string.IsNullOrEmpty(user.Organization.StoreCode))
        {
            user.Organization.StoreCode = await GenerateStoreCodeAsync();
        }
        
        await _context.SaveChangesAsync();
        
        // Send approval email
        var emailBody = $@"
            <h2>Your Shop is Ready! 🎉</h2>
            <p>Hi {user.FullName},</p>
            <p>Great news! {user.Organization?.ShopName} has been approved and is ready to go.</p>
            <p>Your store code for providers is: <strong>{user.Organization?.StoreCode}</strong></p>
            <p>Share this with consigners so they can register and join your shop.</p>
            <p>Welcome to ConsignmentGenie!</p>
            <p>- The ConsignmentGenie Team</p>";
        
        await _emailService.SendSimpleEmailAsync(
            user.Email,
            "Your Shop is Ready! 🎉",
            emailBody);
    }

    private async Task<string> GenerateStoreCodeAsync()
    {
        string storeCode;
        do
        {
            // Generate 6-character alphanumeric code
            storeCode = GenerateRandomCode(6);
        }
        while (await _context.Organizations.AnyAsync(o => o.StoreCode == storeCode));
        
        return storeCode;
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    public async Task RejectOwnerAsync(Guid userId, Guid rejectedByUserId, string? reason)
    {
        await RejectUserAsync(userId, rejectedByUserId, reason);
    }

    private static string GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private static string GetLastName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    private async Task<string> GenerateProviderNumberAsync(Guid organizationId)
    {
        var count = await _context.Consignors
            .CountAsync(p => p.OrganizationId == organizationId);
        return $"PRV-{(count + 1):D5}";
    }

    public async Task<InvitationValidationDto> ValidateInvitationTokenAsync(string token)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Validating invitation token: {Token}", token?.Substring(0, Math.Min(token?.Length ?? 0, 8)) + "...");

        var invitation = await _context.ConsignorInvitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
        {
            _logger.LogWarning("[PROVIDER_INVITATION] Invitation token not found: {Token}", token?.Substring(0, Math.Min(token?.Length ?? 0, 8)) + "...");
            return new InvitationValidationDto
            {
                IsValid = false,
                Message = "Invalid invitation link"
            };
        }

        _logger.LogDebug("[PROVIDER_INVITATION] Found invitation for {Email} in organization {OrganizationName} (ID: {OrganizationId}), expires: {ExpirationDate}",
            invitation.Email, invitation.Organization.Name, invitation.OrganizationId, invitation.ExpirationDate);

        if (invitation.ExpirationDate <= DateTime.UtcNow)
        {
            _logger.LogWarning("[PROVIDER_INVITATION] Invitation token expired for {Email} - expired on {ExpirationDate}",
                invitation.Email, invitation.ExpirationDate);
            return new InvitationValidationDto
            {
                IsValid = false,
                Message = "This invitation has expired"
            };
        }

        // Check if already used (user exists with this email)
        _logger.LogDebug("[PROVIDER_INVITATION] Checking if user with email {Email} already exists", invitation.Email);
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == invitation.Email);

        if (existingUser != null)
        {
            _logger.LogWarning("[PROVIDER_INVITATION] User with email {Email} already exists - invitation cannot be used",
                invitation.Email);
            return new InvitationValidationDto
            {
                IsValid = false,
                Message = "An account with this email already exists"
            };
        }

        _logger.LogInformation("[PROVIDER_INVITATION] Invitation token validation successful for {Email} in organization {OrganizationName}",
            invitation.Email, invitation.Organization.Name);

        // Split the stored name into first and last names
        var nameParts = invitation.Name?.Trim().Split(' ', 2) ?? Array.Empty<string>();
        var firstName = nameParts.Length > 0 ? nameParts[0] : "";
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        return new InvitationValidationDto
        {
            IsValid = true,
            ShopName = invitation.Organization.ShopName ?? invitation.Organization.Name,
            InvitedFirstName = firstName,
            InvitedLastName = lastName,
            InvitedEmail = invitation.Email,
            ExpirationDate = invitation.ExpirationDate
        };
    }

    public async Task<RegistrationResultDto> RegisterConsignorFromInvitationAsync(RegisterConsignorFromInvitationRequest request)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Starting provider registration from invitation for email {Email}", request.Email);
        _logger.LogDebug("[PROVIDER_INVITATION] Invitation registration details: FirstName={FirstName}, LastName={LastName}, Phone={Phone}, Address={Address}",
            request.FirstName, request.LastName, request.Phone, request.Address);

        try
        {
            // Validate the invitation first
            _logger.LogDebug("[PROVIDER_INVITATION] Validating invitation token for registration");
            var validation = await ValidateInvitationTokenAsync(request.InvitationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("[PROVIDER_INVITATION] Invitation validation failed for {Email}: {ValidationMessage}",
                    request.Email, validation.Message);
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = validation.Message ?? "Invalid invitation"
                };
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Invitation validated successfully for {Email} joining {ShopName}",
                request.Email, validation.ShopName);

            _logger.LogDebug("[PROVIDER_INVITATION] Retrieving invitation details from database");
            var invitation = await _context.ConsignorInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == request.InvitationToken);

            if (invitation == null)
            {
                _logger.LogError("[PROVIDER_INVITATION] Invitation not found in database for token");
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Invitation not found"
                };
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Creating user account for invited provider {Email} in organization {OrganizationName} (ID: {OrganizationId})",
                request.Email, invitation.Organization.Name, invitation.OrganizationId);

            // Create the user
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = $"{request.FirstName} {request.LastName}".Trim(),
                Phone = request.Phone,
                Role = UserRole.Consignor,
                OrganizationId = invitation.OrganizationId,
                ApprovalStatus = ApprovalStatus.Approved, // Auto-approve invited providers
                ApprovedBy = invitation.InvitedById,
                ApprovedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[PROVIDER_INVITATION] User account created successfully for {Email} with ID {UserId}", request.Email, user.Id);

            // Create the provider record
            _logger.LogInformation("[PROVIDER_INVITATION] Creating Consignor record for {Email}", request.Email);
            var consignorNumber = await GenerateProviderNumberAsync(invitation.OrganizationId);
            var provider = new Consignor
            {
                OrganizationId = invitation.OrganizationId,
                UserId = user.Id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                PreferredPaymentMethod = "Check", // Default
                Status = ConsignorStatus.Active,
                ApprovalStatus = "Approved",
                ApprovedBy = invitation.InvitedById,
                ConsignorNumber = consignorNumber
            };

            _context.Consignors.Add(provider);

            // Mark invitation as used
            _logger.LogDebug("[PROVIDER_INVITATION] Marking invitation as used for {Email}", request.Email);
            invitation.IsUsed = true;
            invitation.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("[PROVIDER_INVITATION] Consignor record created successfully with number {ConsignorNumber} for {Email}",
                consignorNumber, request.Email);

            // Create ConsignorAgreement if organization has agreement requirements
            await CreateConsignorAgreementIfRequiredAsync(provider, invitation.Organization, $"{request.FirstName} {request.LastName}".Trim(), request.Email);

            // Send welcome email to provider
            _logger.LogInformation("[PROVIDER_INVITATION] Sending welcome email to new provider {Email}", request.Email);
            var consignorEmailBody = $@"
                <h2>Welcome to {validation.ShopName}! 🎉</h2>
                <p>Hi {request.FirstName} {request.LastName},</p>
                <p>Your provider account has been successfully created for {validation.ShopName}.</p>
                <p>You can now log in to your Consignor Portal to:</p>
                <ul>
                    <li>View your consigned items</li>
                    <li>Track sales and earnings</li>
                    <li>Manage your account settings</li>
                </ul>
                <p>Welcome aboard!</p>
                <p>- The ConsignmentGenie Team</p>";

            var consignorEmailResult = await _emailService.SendSimpleEmailAsync(
                request.Email,
                "Welcome to ConsignmentGenie! 🎉",
                consignorEmailBody);
            _logger.LogInformation("[PROVIDER_INVITATION] Welcome email sent to provider {Email}: {EmailResult}",
                request.Email, consignorEmailResult);

            // Create agreement notification for consignor to acknowledge
            _logger.LogInformation("[AGREEMENT_DEBUG] Creating agreement notification for invitation registration: {Email}", request.Email);
            // NOTE: Agreement handling is now done through upload workflow, not email notifications
            _logger.LogInformation("[AGREEMENT_DEBUG] Completed agreement notification creation for invitation registration: {Email}", request.Email);

            // Notify the shop owner
            _logger.LogDebug("[PROVIDER_INVITATION] Looking up shop owner (ID: {InvitedById}) to notify about new provider",
                invitation.InvitedById);
            var ownerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == invitation.InvitedById);

            if (ownerUser != null)
            {
                _logger.LogInformation("[PROVIDER_INVITATION] Sending shop owner notification to {OwnerEmail} about new provider {ProviderEmail}",
                    ownerUser.Email, request.Email);
                var ownerEmailBody = $@"
                    <h2>Consignor Joined Your Shop</h2>
                    <p>Hi {validation.ShopName},</p>
                    <p>{request.FirstName} {request.LastName} has successfully completed their registration and joined your shop.</p>
                    <p><strong>Consignor Details:</strong></p>
                    <ul>
                        <li>Name: {request.FirstName} {request.LastName}</li>
                        <li>Email: {request.Email}</li>
                        <li>Phone: {request.Phone ?? "Not provided"}</li>
                    </ul>
                    <p>They can now start adding items to consign with your shop.</p>
                    <p>- ConsignmentGenie</p>";

                var ownerEmailResult = await _emailService.SendSimpleEmailAsync(
                    ownerUser.Email,
                    $"New Consignor Joined - {request.FirstName} {request.LastName}",
                    ownerEmailBody);
                _logger.LogInformation("[PROVIDER_INVITATION] Shop owner notification sent to {OwnerEmail}: {EmailResult}",
                    ownerUser.Email, ownerEmailResult);
            }
            else
            {
                _logger.LogWarning("[PROVIDER_INVITATION] Could not find shop owner (ID: {InvitedById}) to notify about new provider {ProviderEmail}",
                    invitation.InvitedById, request.Email);
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Consignor registration from invitation completed successfully for {Email} - provider number {ConsignorNumber}",
                request.Email, consignorNumber);

            return new RegistrationResultDto
            {
                Success = true,
                Message = "Registration completed successfully! You can now log in."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROVIDER_INVITATION] Consignor registration from invitation failed for {Email}", request.Email);
            return new RegistrationResultDto
            {
                Success = false,
                Message = "An error occurred during registration.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL unique constraint violation
        return ex.InnerException?.Message.Contains("unique constraint") == true
            || ex.InnerException?.Message.Contains("duplicate key") == true
            || ex.InnerException?.Message.Contains("23505") == true;
    }

    // OLD EMAIL AGREEMENT METHOD REMOVED - Agreement handling is now done through upload workflow

    private async Task CreateConsignorAgreementIfRequiredAsync(
        Consignor consignor,
        Organization organization,
        string fullName,
        string email)
    {
        try
        {
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
            var status = "pending";
            var method = organization.AgreementMethod switch
            {
                "acknowledge" => "consignor_acknowledge",
                "upload" => "consignor_upload",
                _ => "consignor_acknowledge" // default fallback
            };

            // Create ConsignorAgreement record
            var agreement = new ConsignorAgreement
            {
                ConsignorId = consignor.Id,
                OrganizationId = organization.Id,
                Status = status,
                Method = method,
                Notes = processedText // Store the processed agreement text
            };

            _context.ConsignorAgreements.Add(agreement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("[AGREEMENT_CREATION] ConsignorAgreement created successfully for consignor {ConsignorId}, method: {Method}, status: {Status}",
                consignor.Id, method, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AGREEMENT_CREATION] Failed to create ConsignorAgreement for consignor {ConsignorId} in organization {OrganizationId}",
                consignor.Id, organization.Id);
            // Don't throw - agreement creation failure shouldn't block registration
        }
    }
}