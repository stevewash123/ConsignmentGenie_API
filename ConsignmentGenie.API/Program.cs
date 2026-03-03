using Azure.Storage.Blobs;
using ConsignmentGenie.API.Hubs;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Application.Interfaces;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Core.Services;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Infrastructure.Repositories;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Sinks.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for console and PostgreSQL logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.PostgreSQL(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
        tableName: "Logs",
        needAutoCreateTable: true)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure port for deployment compatibility
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure property names to use camelCase (e.g., "publicToken" instead of "PublicToken")
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Configure enums to serialize as camelCase strings (e.g., "upload" instead of "Upload")
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ConsignmentGenie API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// Database configuration
builder.Services.AddDbContext<ConsignmentGenieContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? "ConsignmentGenie_Super_Secret_Key_2024_32_Characters_Long!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // Configure SignalR authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Memory Cache for settings
builder.Services.AddMemoryCache();

// HTTP Context Accessor for JWT claims access
builder.Services.AddHttpContextAccessor();

// External services

builder.Services.AddSingleton<BlobServiceClient>(provider =>
    new BlobServiceClient(builder.Configuration["Azure:Storage:ConnectionString"]));

// Hangfire for background jobs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// HttpClient for external API calls
builder.Services.AddHttpClient<IQuickBooksService, QuickBooksService>();

// Repository pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddScoped<IClearDateCalculator, ClearDateCalculator>();
builder.Services.AddScoped<IPayoutSummaryComputationService, ConsignmentGenie.Application.Services.PayoutSummaryComputationService>();
builder.Services.AddScoped<IPayoutNotificationService, ConsignmentGenie.Application.Services.PayoutNotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IOrganizationSettingsManager, OrganizationSettingsManager>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISplitCalculationService, SplitCalculationService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<ISubscriptionService, ConsignmentGenie.Application.Services.SubscriptionService>();
builder.Services.AddScoped<IFeatureGatingService, FeatureGatingService>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();

// Enhanced notification services with caching and compliance
builder.Services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
builder.Services.AddScoped<IEmailComplianceService, EmailComplianceService>();
builder.Services.AddScoped<INotificationBatchService, NotificationBatchService>();

// Centralized default settings management
builder.Services.AddScoped<IDefaultSettingsService, DefaultSettingsService>();

// Use enhanced notification service instead of original
builder.Services.AddScoped<ConsignmentGenie.Core.Interfaces.INotificationService, EnhancedNotificationService>();
builder.Services.AddScoped<IConsignorNotificationService, ConsignorNotificationService>();
builder.Services.AddScoped<ConsolidatedNotificationService>();
builder.Services.AddScoped<ConsolidatedNotificationJob>();
builder.Services.AddScoped<IConsignorInvitationService, ConsignorInvitationService>();
builder.Services.AddScoped<IOwnerInvitationService, OwnerInvitationService>();
builder.Services.AddScoped<IClerkInvitationService, ClerkInvitationService>();
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<ISuggestionService, SuggestionService>();
// Report services (focused single-responsibility services)
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IInventoryReportService, InventoryReportService>();
builder.Services.AddScoped<IPayoutReportService, PayoutReportService>();
builder.Services.AddScoped<IConsignorReportService, ConsignorReportService>();
builder.Services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IStoreCodeService, StoreCodeService>();
builder.Services.AddScoped<ISetupWizardService, SetupWizardService>();
builder.Services.AddScoped<IAgreementGeneratorService, AgreementGeneratorService>();
builder.Services.AddScoped<IOwnerApprovalService, OwnerApprovalService>();
builder.Services.AddScoped<IItemImportService, ItemImportService>();
builder.Services.AddScoped<IPendingImportItemService, PendingImportItemService>();
builder.Services.AddScoped<IFileUploadTrackingService, FileUploadTrackingService>();
builder.Services.AddScoped<PendingImportAssignmentService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<StatementGenerationJob>();

// Payment services for non-Square POS
builder.Services.AddScoped<IStripeConnectService, StripeConnectService>();  // Keep for non-Square POS

// Photo storage implementations
builder.Services.AddScoped<CloudinaryPhotoService>();
builder.Services.AddScoped<AzurePhotoService>();
builder.Services.AddScoped<IPhotoService, PhotoService>(); // Factory that switches between implementations

// Document storage service for agreements
builder.Services.AddScoped<IDocumentService, CloudinaryDocumentService>();

// Payment gateway implementations
builder.Services.AddScoped<StripePaymentGatewayService>();
builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

// Accounting service implementations
builder.Services.AddScoped<IQuickBooksService, QuickBooksService>();
builder.Services.AddScoped<QuickBooksAccountingService>();

// Square integration services
builder.Services.AddScoped<ISquareOAuthService, SquareOAuthService>();
builder.Services.AddScoped<ISquareService, SquareService>();
builder.Services.AddScoped<ISquareInventoryService, SquareInventoryService>();
builder.Services.AddScoped<ISquareSyncService, ConsignmentGenie.Application.Services.Square.SquareSyncService>();

// Sales tax service implementations - register based on configuration
var salesTaxProvider = builder.Configuration.GetValue<string>("SalesTax:Provider", "manual");
switch (salesTaxProvider.ToLowerInvariant())
{
    case "mock":
        builder.Services.AddScoped<ISalesTaxService, MockSalesTaxService>();
        break;
    case "manual":
    default:
        builder.Services.AddScoped<ISalesTaxService, ManualSalesTaxService>();
        break;
    // Future: case "api": builder.Services.AddScoped<ISalesTaxService, ApiSalesTaxService>(); break;
}

// Add SignalR
builder.Services.AddSignalR();

// Register job notification service
builder.Services.AddScoped<IJobNotificationService, ConsignmentGenie.API.Services.JobNotificationService>();

// Financial services for auto-payouts
builder.Services.AddHttpClient<IPlaidService, ConsignmentGenie.Application.Services.Financial.PlaidService>();
builder.Services.AddHttpClient<IDwollaService, ConsignmentGenie.Application.Services.Financial.DwollaService>();
builder.Services.AddHttpClient<IACHService, ConsignmentGenie.Application.Services.Financial.ACHService>();

// QuickBooks sync services
builder.Services.AddSingleton(new ConsignmentGenie.Core.Services.QBApiConfig
{
    BaseUrl = builder.Configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com",
    ClientId = builder.Configuration["QuickBooks:ClientId"] ?? "",
    ClientSecret = builder.Configuration["QuickBooks:ClientSecret"] ?? "",
    RedirectUri = builder.Configuration["QuickBooks:RedirectUri"] ?? "",
    IsSandbox = bool.Parse(builder.Configuration["QuickBooks:IsSandbox"] ?? "true")
});
builder.Services.AddScoped<IQuickBooksApiService, ConsignmentGenie.Application.Services.QuickBooks.QuickBooksApiService>();
builder.Services.AddScoped<IQBSyncService, ConsignmentGenie.Application.Services.QuickBooks.QBSyncService>();

// Reservation System services
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ISmsService, ConsoleSmsService>(); // Use ConsoleSmsService for development
builder.Services.AddScoped<ICustomerAuthService, CustomerAuthService>();
builder.Services.AddScoped<IBusinessHoursService, BusinessHoursService>();
builder.Services.AddScoped<ICategoryValidationService, CategoryValidationService>();

// Background job classes
builder.Services.AddScoped<ConsignmentGenie.Application.Jobs.AutoPayoutJob>();
builder.Services.AddScoped<ConsignmentGenie.Application.Jobs.QuickBooks.QBSyncPayoutJob>();
builder.Services.AddScoped<ConsignmentGenie.Application.Jobs.QuickBooks.QBSyncSaleJob>();
builder.Services.AddScoped<ConsignmentGenie.Application.Jobs.ItemExpirationJob>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConsignmentGenie API v1");
    });
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new ConsignmentGenie.API.Infrastructure.HangfireAuthorizationFilter() }
    });
}

// Middleware order is important
app.UseCors();

// Static file serving removed - using Cloudinary for all file uploads

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR hub endpoints
app.MapHub<JobNotificationHub>("/hubs/jobs");
app.MapHub<NotificationHub>("/hubs/notifications");

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Schedule background jobs after Hangfire is initialized
app.MapHangfireDashboard();

// Use a startup filter to schedule jobs after the application starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Schedule item expiration processing job (runs daily at 1:00 AM)
    recurringJobManager.AddOrUpdate<ConsignmentGenie.Application.Jobs.ItemExpirationJob>(
        "process-item-expirations",
        job => job.ProcessItemExpirationsAsync(),
        "0 1 * * *"); // Cron expression: 1:00 AM daily

    // Cart cleanup job removed - no longer needed after storefront removal

    // Schedule monthly statement generation job (runs on 1st of each month at 2 AM)
    recurringJobManager.AddOrUpdate<StatementGenerationJob>(
        "generate-monthly-statements",
        job => job.GenerateMonthlyStatementsAsync(),
        "0 2 1 * *"); // Cron expression: 2 AM on the 1st day of every month

    // Schedule payout summary computation job (runs daily at 2:00 AM)
    recurringJobManager.AddOrUpdate<IPayoutSummaryComputationService>(
        "compute-payout-summaries",
        service => service.ComputeAllSummariesAsync(CancellationToken.None),
        "0 2 * * *"); // Cron expression: 2:00 AM daily

    // Schedule payout notification job (runs daily at 2:30 AM, after summaries are computed)
    recurringJobManager.AddOrUpdate<IPayoutNotificationService>(
        "send-payout-notifications",
        service => service.SendReadyForPayoutNotificationsAsync(CancellationToken.None),
        "30 2 * * *"); // Cron expression: 2:30 AM daily

    // Enhanced notification batch processing jobs

    // Daily batch notifications (3:00 AM daily - after payout notifications)
    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-daily-batch-notifications",
        service => service.ProcessDailyBatchAsync(null),
        "0 3 * * *"); // Cron expression: 3:00 AM daily

    // Weekly batch notifications (3:30 AM on each day of the week)
    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-monday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Monday, null),
        "30 3 * * 1"); // Monday at 3:30 AM

    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-tuesday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Tuesday, null),
        "30 3 * * 2"); // Tuesday at 3:30 AM

    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-wednesday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Wednesday, null),
        "30 3 * * 3"); // Wednesday at 3:30 AM

    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-thursday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Thursday, null),
        "30 3 * * 4"); // Thursday at 3:30 AM

    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-friday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Friday, null),
        "30 3 * * 5"); // Friday at 3:30 AM

    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-saturday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Saturday, null),
        "30 3 * * 6"); // Saturday at 3:30 AM

    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-weekly-batch-sunday",
        service => service.ProcessWeeklyBatchAsync(DayOfWeek.Sunday, null),
        "30 3 * * 0"); // Sunday at 3:30 AM

    // Monthly batch notifications (4:00 AM on the 1st of each month)
    recurringJobManager.AddOrUpdate<INotificationBatchService>(
        "process-monthly-batch-notifications",
        service => service.ProcessMonthlyBatchAsync(null),
        "0 4 1 * *"); // Cron expression: 4:00 AM on 1st day of month

    // Note: Auto-payout jobs are scheduled individually per organization
    // They are created when organizations enable auto-payouts in their settings
    // Individual schedules are managed by the payout settings service
});

try
{
    Log.Information("Starting ConsignmentGenie API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
