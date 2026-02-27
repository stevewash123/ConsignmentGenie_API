using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using Stripe;
using System.Security.Cryptography;
using System.Text;

namespace ConsignmentGenie.Application.Services;

public class StripeConnectService : IStripeConnectService
{
    private readonly ConsignmentGenieContext _context;
    private readonly ILogger<StripeConnectService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public StripeConnectService(
        ConsignmentGenieContext context,
        ILogger<StripeConnectService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        // Get Stripe Connect configuration
        _clientId = _configuration["Stripe:Connect:ClientId"] ?? throw new InvalidOperationException("Stripe Connect Client ID not configured");
        _clientSecret = _configuration["Stripe:Connect:ClientSecret"] ?? throw new InvalidOperationException("Stripe Connect Client Secret not configured");
        _redirectUri = _configuration["Stripe:Connect:RedirectUri"] ?? throw new InvalidOperationException("Stripe Connect Redirect URI not configured");

        // Set Stripe API key
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe Secret Key not configured");
    }

    public async Task<string> GenerateConnectLinkAsync(Guid organizationId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organization not found");
            }

            // Generate a secure state parameter
            var state = GenerateSecureState(organizationId);

            // Build Stripe Connect OAuth URL
            var connectUrl = $"https://connect.stripe.com/oauth/authorize" +
                $"?response_type=code" +
                $"&client_id={_clientId}" +
                $"&scope=read_write" +
                $"&redirect_uri={Uri.EscapeDataString(_redirectUri)}" +
                $"&state={state}" +
                $"&stripe_user[email]={Uri.EscapeDataString(organization.ShopEmail ?? string.Empty)}" +
                $"&stripe_user[business_name]={Uri.EscapeDataString(organization.ShopName ?? organization.Name)}";

            _logger.LogInformation("Generated Stripe Connect link for organization {OrganizationId}", organizationId);

            return connectUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Stripe Connect link for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<bool> HandleConnectCallbackAsync(string authorizationCode, string state)
    {
        try
        {
            // Verify state parameter and extract organization ID
            var organizationId = VerifyAndExtractState(state);
            if (organizationId == Guid.Empty)
            {
                _logger.LogError("Invalid state parameter in Stripe Connect callback: {State}", state);
                return false;
            }

            // Exchange authorization code for access token
            var tokenService = new OAuthTokenService();
            var response = await tokenService.CreateAsync(new OAuthTokenCreateOptions
            {
                GrantType = "authorization_code",
                Code = authorizationCode,
                ClientSecret = _clientSecret
            });

            // Update organization with Stripe Connect details
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                _logger.LogError("Organization {OrganizationId} not found during Stripe Connect callback", organizationId);
                return false;
            }

            organization.StripeConnected = true;
            organization.StripeAccountId = response.StripeUserId;
            organization.StripeAccessToken = response.AccessToken; // TODO: Encrypt in production
            organization.StripeRefreshToken = response.RefreshToken; // TODO: Encrypt in production
            organization.StripePublishableKey = response.StripePublishableKey;
            organization.StripeConnectedAt = DateTime.UtcNow;
            organization.StripePayoutsEnabled = response.StripePublishableKey != null; // Basic check

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully connected Stripe account {AccountId} for organization {OrganizationId}",
                response.StripeUserId, organizationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe Connect callback with code {AuthCode}", authorizationCode);
            return false;
        }
    }

    public async Task<string> CreatePaymentIntentAsync(Guid organizationId, decimal amount, string currency = "usd", Dictionary<string, string>? metadata = null)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organization not found");
            }

            if (!organization.StripeConnected || string.IsNullOrEmpty(organization.StripeAccountId))
            {
                throw new InvalidOperationException("Organization does not have Stripe Connect configured");
            }

            // Create payment intent with connected account
            var service = new PaymentIntentService();
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = metadata ?? new Dictionary<string, string>(),
                TransferData = new PaymentIntentTransferDataOptions
                {
                    Destination = organization.StripeAccountId
                }
            };

            // Add organization info to metadata
            options.Metadata["organization_id"] = organizationId.ToString();
            options.Metadata["shop_name"] = organization.ShopName ?? organization.Name;

            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Created payment intent {PaymentIntentId} for {Amount} {Currency} for organization {OrganizationId}",
                paymentIntent.Id, amount, currency, organizationId);

            return paymentIntent.ClientSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for organization {OrganizationId}, amount {Amount}",
                organizationId, amount);
            throw;
        }
    }

    public async Task<string> GetPaymentIntentStatusAsync(string paymentIntentId, Guid organizationId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            return paymentIntent.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment intent status for {PaymentIntentId}", paymentIntentId);
            throw;
        }
    }

    public async Task<bool> ConfirmPaymentIntentAsync(string paymentIntentId, Guid organizationId)
    {
        try
        {
            var service = new PaymentIntentService();
            var options = new PaymentIntentConfirmOptions();

            var paymentIntent = await service.ConfirmAsync(paymentIntentId, options);

            return paymentIntent.Status == "succeeded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment intent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<StripeConnectAccountStatus> GetAccountStatusAsync(Guid organizationId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organization not found");
            }

            var status = new StripeConnectAccountStatus
            {
                IsConnected = organization.StripeConnected,
                AccountId = organization.StripeAccountId,
                PublishableKey = organization.StripePublishableKey,
                ConnectedAt = organization.StripeConnectedAt
            };

            // If connected, get detailed account info from Stripe
            if (organization.StripeConnected && !string.IsNullOrEmpty(organization.StripeAccountId))
            {
                try
                {
                    var accountService = new Stripe.AccountService();
                    var account = await accountService.GetAsync(organization.StripeAccountId);

                    status.PayoutsEnabled = account.PayoutsEnabled;
                    status.ChargesEnabled = account.ChargesEnabled;
                    status.RequirementsPending = account.Requirements?.CurrentlyDue?.ToList() ?? new List<string>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve detailed account status for {AccountId}", organization.StripeAccountId);
                    // Return basic status from database
                    status.PayoutsEnabled = organization.StripePayoutsEnabled;
                    status.ChargesEnabled = organization.StripeConnected;
                }
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe Connect account status for organization {OrganizationId}", organizationId);
            throw;
        }
    }

    public async Task<bool> DisconnectAccountAsync(Guid organizationId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
            {
                throw new ArgumentException("Organization not found");
            }

            // Clear Stripe Connect information
            organization.StripeConnected = false;
            organization.StripeAccountId = null;
            organization.StripeAccessToken = null;
            organization.StripeRefreshToken = null;
            organization.StripePublishableKey = null;
            organization.StripeConnectedAt = null;
            organization.StripePayoutsEnabled = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Disconnected Stripe Connect account for organization {OrganizationId}", organizationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting Stripe Connect account for organization {OrganizationId}", organizationId);
            return false;
        }
    }

    private string GenerateSecureState(Guid organizationId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var data = $"{organizationId}:{timestamp}";

        // Create a hash of the data with a secret
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_clientSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var hashString = Convert.ToBase64String(hash);

        // Combine data and hash
        var state = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{data}:{hashString}"));

        return state;
    }

    private Guid VerifyAndExtractState(string state)
    {
        try
        {
            var decodedState = Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var parts = decodedState.Split(':');

            if (parts.Length != 3)
                return Guid.Empty;

            var organizationId = parts[0];
            var timestamp = parts[1];
            var providedHash = parts[2];

            // Verify timestamp (within 1 hour)
            if (!long.TryParse(timestamp, out var unixTimestamp))
                return Guid.Empty;

            var stateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            if (DateTimeOffset.UtcNow.Subtract(stateTime).TotalHours > 1)
                return Guid.Empty;

            // Verify hash
            var data = $"{organizationId}:{timestamp}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_clientSecret));
            var expectedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));

            if (providedHash != expectedHash)
                return Guid.Empty;

            return Guid.TryParse(organizationId, out var guid) ? guid : Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}