using ConsignmentGenie.Core.DTOs.Registration;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ConsignmentGenie.Application.Services;

public class RegistrationService : IRegistrationService
{
    private readonly ConsignmentGenieContext _context;
    private readonly IEmailService _emailService;
    private readonly IStoreCodeService _storeCodeService;
    private readonly IAuthService _authService;

    public RegistrationService(
        ConsignmentGenieContext context,
        IEmailService emailService,
        IStoreCodeService storeCodeService,
        IAuthService authService)
    {
        _context = context;
        _emailService = emailService;
        _storeCodeService = storeCodeService;
        _authService = authService;
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
        try
        {
            // Validate store code
            var validation = await ValidateStoreCodeAsync(request.StoreCode);
            if (!validation.IsValid)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = validation.ErrorMessage ?? "Invalid store code",
                    Errors = new List<string> { "Invalid store code" }
                };
            }

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

            // Get organization
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.StoreCode == request.StoreCode);

            if (organization == null)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Invalid store code",
                    Errors = new List<string> { "Store code not found" }
                };
            }

            // Create user
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

            // If auto-approved, create Provider record immediately
            if (organization.AutoApproveProviders)
            {
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
            }

            // Send confirmation email to provider
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

            await _emailService.SendSimpleEmailAsync(
                request.Email,
                organization.AutoApproveProviders ? "Account Approved - You're In! 🎉" : "Welcome to ConsignmentGenie - Account Pending",
                providerEmailBody);

            // Send notification to owner if not auto-approved
            if (!organization.AutoApproveProviders)
            {
                var ownerUsers = await _context.Users
                    .Where(u => u.OrganizationId == organization.Id && u.Role == UserRole.Owner)
                    .ToListAsync();

                foreach (var owner in ownerUsers)
                {
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

                    await _emailService.SendSimpleEmailAsync(
                        owner.Email,
                        $"New Provider Request - {request.FullName}",
                        ownerEmailBody);
                }
            }

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
        // 🏗️ AGGREGATE ROOT PATTERN: Detach all tracked entities to avoid conflicts
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        // Load user with organization for business logic
        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new ArgumentException("User not found");

        if (user.ApprovalStatus != ApprovalStatus.Pending)
            throw new InvalidOperationException("User is not pending approval");

        // Update user status
        user.ApprovalStatus = ApprovalStatus.Approved;
        user.ApprovedBy = approvedByUserId;
        user.ApprovedAt = DateTime.UtcNow;

        // 🏗️ AGGREGATE ROOT PATTERN: If provider, create provider record
        if (user.Role == UserRole.Provider)
        {
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
                ProviderNumber = await GenerateProviderNumberAsync(user.OrganizationId)
            };

            _context.Providers.Add(provider);
        }

        await _context.SaveChangesAsync();

        // Send approval email
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

        await _emailService.SendSimpleEmailAsync(
            user.Email,
            user.Role == UserRole.Provider ? "Account Approved - You're In! 🎉" : "Your Shop is Ready! 🎉",
            emailBody);
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
        var invitation = await _context.ProviderInvitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
        {
            return new InvitationValidationDto
            {
                IsValid = false,
                Message = "Invalid invitation link"
            };
        }

        if (invitation.ExpirationDate <= DateTime.UtcNow)
        {
            return new InvitationValidationDto
            {
                IsValid = false,
                Message = "This invitation has expired"
            };
        }

        // Check if already used (user exists with this email)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == invitation.Email);

        if (existingUser != null)
        {
            return new InvitationValidationDto
            {
                IsValid = false,
                Message = "An account with this email already exists"
            };
        }

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
        try
        {
            // Validate the invitation first
            var validation = await ValidateInvitationTokenAsync(request.InvitationToken);
            if (!validation.IsValid)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = validation.Message ?? "Invalid invitation"
                };
            }

            var invitation = await _context.ProviderInvitations
                .Include(i => i.Organization)
                .FirstOrDefaultAsync(i => i.Token == request.InvitationToken);

            if (invitation == null)
            {
                return new RegistrationResultDto
                {
                    Success = false,
                    Message = "Invitation not found"
                };
            }

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

            // Create the provider record
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
                ProviderNumber = await GenerateProviderNumberAsync(invitation.OrganizationId)
            };

            _context.Providers.Add(provider);

            // Mark invitation as used
            invitation.IsUsed = true;
            invitation.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send welcome email to provider
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

            await _emailService.SendSimpleEmailAsync(
                request.Email,
                "Welcome to ConsignmentGenie! 🎉",
                providerEmailBody);

            // Notify the shop owner
            var ownerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == invitation.InvitedById);

            if (ownerUser != null)
            {
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

                await _emailService.SendSimpleEmailAsync(
                    ownerUser.Email,
                    $"New Provider Joined - {request.FullName}",
                    ownerEmailBody);
            }

            return new RegistrationResultDto
            {
                Success = true,
                Message = "Registration completed successfully! You can now log in."
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

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL unique constraint violation
        return ex.InnerException?.Message.Contains("unique constraint") == true
            || ex.InnerException?.Message.Contains("duplicate key") == true
            || ex.InnerException?.Message.Contains("23505") == true;
    }
}