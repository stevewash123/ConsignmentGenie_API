using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace ConsignmentGenie.Application.Services;

public class RegistrationService : IRegistrationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IStoreCodeService _storeCodeService;
    private readonly IAuthService _authService;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        ConsignmentGenieContext context,
        IEmailService emailService,
        IStoreCodeService storeCodeService,
        IAuthService authService,
        ILogger<RegistrationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _storeCodeService = storeCodeService;
        _authService = authService;
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

            // Check if subdomain/slug already exists
            var existingOrganization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Subdomain == request.Subdomain || o.Slug == request.Subdomain);

            if (existingOrganization != null)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "This shop name is already taken. Please choose a different one.",
                    Errors = new List<string> { "Subdomain already in use" }
                };
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
                        Subdomain = request.Subdomain,
                        Slug = request.Subdomain, // Use subdomain as slug for now
                        StoreCode = _storeCodeService.GenerateStoreCode(),
                        StoreCodeEnabled = true,
                        Status = "active" // Auto-approve for immediate setup
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

            // Send welcome email to owner
            var ownerEmailBody = $@"
                <h2>Welcome to ConsignmentGenie!</h2>
                <p>Hi {request.FullName},</p>
                <p>Your shop ""{request.ShopName}"" is now ready to go!</p>
                <p>You can log in and start:</p>
                <ul>
                    <li>Adding providers (consigners)</li>
                    <li>Setting up your shop details and commission rates</li>
                    <li>Managing inventory and transactions</li>
                </ul>
                <p>Your store code for providers: <strong>{organization.StoreCode}</strong></p>
                <p>Questions? Reply to this email.</p>
                <p>- The ConsignmentGenie Team</p>";

            await _emailService.SendSimpleEmailAsync(
                request.Email,
                "Welcome to ConsignmentGenie - Your Shop is Ready!",
                ownerEmailBody);

            // Generate JWT token for immediate authentication
            var token = _authService.GenerateJwtToken(user.Id, user.Email, user.Role.ToString(), user.OrganizationId);
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

    public async Task<RegistrationResultDto> RegisterProviderAsync(RegisterProviderRequest request)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Starting provider registration for email {Email} with store code {StoreCode}",
            request.Email, request.StoreCode);
        _logger.LogDebug("[PROVIDER_INVITATION] Provider registration details: FullName={FullName}, Phone={Phone}, PreferredPayment={PreferredPaymentMethod}",
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

            _logger.LogInformation("[PROVIDER_INVITATION] Found organization {OrganizationName} (ID: {OrganizationId}) for store code {StoreCode}, AutoApprove: {AutoApproveProviders}",
                organization.Name, organization.Id, request.StoreCode, organization.AutoApproveProviders);

            // Create user
            _logger.LogInformation("[PROVIDER_INVITATION] Creating user account for {Email} with role Provider, approval status will be {ApprovalStatus}",
                request.Email, organization.AutoApproveProviders ? "Approved" : "Pending");
            var user = new User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName,
                Phone = request.Phone,
                Role = UserRole.Provider,
                ApprovalStatus = organization.AutoApproveProviders ? ApprovalStatus.Approved : ApprovalStatus.Pending,
                OrganizationId = organization.Id
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[PROVIDER_INVITATION] User {Email} created successfully with ID {UserId}", request.Email, user.Id);

            // If auto-approved, create Provider record immediately
            if (organization.AutoApproveProviders)
            {
                _logger.LogInformation("[PROVIDER_INVITATION] Auto-approval enabled - creating Provider record for {Email}", request.Email);
                var provider = new Provider
                {
                    OrganizationId = organization.Id,
                    UserId = user.Id,
                    FirstName = GetFirstName(request.FullName),
                    LastName = GetLastName(request.FullName),
                    Email = request.Email,
                    Phone = request.Phone,
                    PreferredPaymentMethod = request.PreferredPaymentMethod ?? "Check",
                    PaymentDetails = request.PaymentDetails,
                    Status = ProviderStatus.Active,
                    ApprovalStatus = "Approved",
                    ApprovedBy = null // Auto-approved
                };

                _context.Providers.Add(provider);
                await _context.SaveChangesAsync();
                _logger.LogInformation("[PROVIDER_INVITATION] Provider record created for {Email} with ID {ProviderId}", request.Email, provider.Id);
            }
            else
            {
                _logger.LogInformation("[PROVIDER_INVITATION] Manual approval required - Provider record will be created after approval");
            }

            // Send confirmation email to provider
            _logger.LogInformation("[PROVIDER_INVITATION] Sending confirmation email to provider {Email}", request.Email);
            var providerEmailBody = $@"
                <h2>Welcome to ConsignmentGenie</h2>
                <p>Hi {request.FullName},</p>
                <p>Thanks for registering with ConsignmentGenie!</p>
                <p>Your request to join {validation.ShopName} is {(organization.AutoApproveProviders ? "approved" : "pending approval")}.</p>
                {(organization.AutoApproveProviders
                    ? "<p>You can now log in to your Provider Portal!</p>"
                    : "<p>The shop owner will review your request and you'll receive an email when your account is ready.</p>")}
                <p>Questions? Reply to this email.</p>
                <p>- The ConsignmentGenie Team</p>";

            var emailResult = await _emailService.SendSimpleEmailAsync(
                request.Email,
                organization.AutoApproveProviders ? "Account Approved - You're In! 🎉" : "Welcome to ConsignmentGenie - Account Pending",
                providerEmailBody);
            _logger.LogInformation("[PROVIDER_INVITATION] Provider confirmation email sent to {Email}: {EmailResult}", request.Email, emailResult);

            // Send notification to owner if not auto-approved
            if (!organization.AutoApproveProviders)
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
                        <h2>New Provider Request</h2>
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
                        $"New Provider Request - {request.FullName}",
                        ownerEmailBody);
                    _logger.LogInformation("[PROVIDER_INVITATION] Owner notification sent to {OwnerEmail}: {EmailResult}",
                        owner.Email, ownerEmailResult);
                }
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Provider registration completed successfully for {Email} - AutoApproved: {AutoApproved}",
                request.Email, organization.AutoApproveProviders);

            return new RegistrationResultDto
            {
                Success = true,
                Message = organization.AutoApproveProviders
                    ? "Account created and approved! You can now log in."
                    : "Account created successfully. You'll receive an email when approved."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROVIDER_INVITATION] Provider registration failed for {Email}", request.Email);
            return new RegistrationResultDto
            {
                Success = false,
                Message = "An error occurred during registration.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<List<PendingApprovalDto>> GetPendingProvidersAsync(Guid organizationId)
    {
        var pendingUsers = await _context.Users
            .Where(u => u.OrganizationId == organizationId
                     && u.Role == UserRole.Provider
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
                          && u.Role == UserRole.Provider
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
        if (user.Role == UserRole.Provider)
        {
            _logger.LogInformation("[PROVIDER_INVITATION] Creating Provider record for approved user {Email}", user.Email);
            var providerNumber = await GenerateProviderNumberAsync(user.OrganizationId);
            var provider = new Provider
            {
                OrganizationId = user.OrganizationId,
                UserId = user.Id,
                FirstName = GetFirstName(user.FullName),
                LastName = GetLastName(user.FullName),
                Email = user.Email,
                Phone = user.Phone,
                PreferredPaymentMethod = "Check", // Default, can be updated later
                Status = ProviderStatus.Active,
                ApprovalStatus = "Approved",
                ApprovedBy = approvedByUserId,
                ProviderNumber = providerNumber
            };

            _context.Providers.Add(provider);
            _logger.LogInformation("[PROVIDER_INVITATION] Provider record will be created with number {ProviderNumber} for {Email}",
                providerNumber, user.Email);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("[PROVIDER_INVITATION] User approval completed successfully for {Email} with role {Role}",
            user.Email, user.Role);

        // Send approval email
        _logger.LogInformation("[PROVIDER_INVITATION] Sending approval notification email to {Email} for role {Role}",
            user.Email, user.Role);
        var emailBody = user.Role == UserRole.Provider
            ? $@"
                <h2>Account Approved - You're In! 🎉</h2>
                <p>Hi {user.FullName},</p>
                <p>Great news! Your account has been approved by {user.Organization.ShopName}.</p>
                <p>You can now log in to your Provider Portal to:</p>
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
            user.Role == UserRole.Provider ? "Account Approved - You're In! 🎉" : "Your Shop is Ready! 🎉",
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
        var count = await _context.Providers
            .CountAsync(p => p.OrganizationId == organizationId);
        return $"PRV-{(count + 1):D5}";
    }

    public async Task<InvitationValidationDto> ValidateInvitationTokenAsync(string token)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Validating invitation token: {Token}", token?.Substring(0, Math.Min(token?.Length ?? 0, 8)) + "...");

        var invitation = await _context.ProviderInvitations
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

        return new InvitationValidationDto
        {
            IsValid = true,
            ShopName = invitation.Organization.ShopName ?? invitation.Organization.Name,
            InvitedName = invitation.Name,
            InvitedEmail = invitation.Email,
            ExpirationDate = invitation.ExpirationDate
        };
    }

    public async Task<RegistrationResultDto> RegisterProviderFromInvitationAsync(RegisterProviderFromInvitationRequest request)
    {
        _logger.LogInformation("[PROVIDER_INVITATION] Starting provider registration from invitation for email {Email}", request.Email);
        _logger.LogDebug("[PROVIDER_INVITATION] Invitation registration details: FullName={FullName}, Phone={Phone}, Address={Address}",
            request.FullName, request.Phone, request.Address);

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
            var invitation = await _context.ProviderInvitations
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
                FullName = request.FullName,
                Phone = request.Phone,
                Role = UserRole.Provider,
                OrganizationId = invitation.OrganizationId,
                ApprovalStatus = ApprovalStatus.Approved, // Auto-approve invited providers
                ApprovedBy = invitation.InvitedById,
                ApprovedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("[PROVIDER_INVITATION] User account created successfully for {Email} with ID {UserId}", request.Email, user.Id);

            // Create the provider record
            _logger.LogInformation("[PROVIDER_INVITATION] Creating Provider record for {Email}", request.Email);
            var providerNumber = await GenerateProviderNumberAsync(invitation.OrganizationId);
            var provider = new Provider
            {
                OrganizationId = invitation.OrganizationId,
                UserId = user.Id,
                FirstName = GetFirstName(request.FullName),
                LastName = GetLastName(request.FullName),
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                PreferredPaymentMethod = "Check", // Default
                Status = ProviderStatus.Active,
                ApprovalStatus = "Approved",
                ApprovedBy = invitation.InvitedById,
                ProviderNumber = providerNumber
            };

            _context.Providers.Add(provider);

            // Mark invitation as used
            _logger.LogDebug("[PROVIDER_INVITATION] Marking invitation as used for {Email}", request.Email);
            invitation.IsUsed = true;
            invitation.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("[PROVIDER_INVITATION] Provider record created successfully with number {ProviderNumber} for {Email}",
                providerNumber, request.Email);

            // Send welcome email to provider
            _logger.LogInformation("[PROVIDER_INVITATION] Sending welcome email to new provider {Email}", request.Email);
            var providerEmailBody = $@"
                <h2>Welcome to {validation.ShopName}! 🎉</h2>
                <p>Hi {request.FullName},</p>
                <p>Your provider account has been successfully created for {validation.ShopName}.</p>
                <p>You can now log in to your Provider Portal to:</p>
                <ul>
                    <li>View your consigned items</li>
                    <li>Track sales and earnings</li>
                    <li>Manage your account settings</li>
                </ul>
                <p>Welcome aboard!</p>
                <p>- The ConsignmentGenie Team</p>";

            var providerEmailResult = await _emailService.SendSimpleEmailAsync(
                request.Email,
                "Welcome to ConsignmentGenie! 🎉",
                providerEmailBody);
            _logger.LogInformation("[PROVIDER_INVITATION] Welcome email sent to provider {Email}: {EmailResult}",
                request.Email, providerEmailResult);

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
                    <h2>Provider Joined Your Shop</h2>
                    <p>Hi {validation.ShopName},</p>
                    <p>{request.FullName} has successfully completed their registration and joined your shop.</p>
                    <p><strong>Provider Details:</strong></p>
                    <ul>
                        <li>Name: {request.FullName}</li>
                        <li>Email: {request.Email}</li>
                        <li>Phone: {request.Phone ?? "Not provided"}</li>
                    </ul>
                    <p>They can now start adding items to consign with your shop.</p>
                    <p>- ConsignmentGenie</p>";

                var ownerEmailResult = await _emailService.SendSimpleEmailAsync(
                    ownerUser.Email,
                    $"New Provider Joined - {request.FullName}",
                    ownerEmailBody);
                _logger.LogInformation("[PROVIDER_INVITATION] Shop owner notification sent to {OwnerEmail}: {EmailResult}",
                    ownerUser.Email, ownerEmailResult);
            }
            else
            {
                _logger.LogWarning("[PROVIDER_INVITATION] Could not find shop owner (ID: {InvitedById}) to notify about new provider {ProviderEmail}",
                    invitation.InvitedById, request.Email);
            }

            _logger.LogInformation("[PROVIDER_INVITATION] Provider registration from invitation completed successfully for {Email} - provider number {ProviderNumber}",
                request.Email, providerNumber);

            return new RegistrationResultDto
            {
                Success = true,
                Message = "Registration completed successfully! You can now log in."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PROVIDER_INVITATION] Provider registration from invitation failed for {Email}", request.Email);
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
}