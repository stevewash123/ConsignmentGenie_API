using Azure.Storage.Blobs;
using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.Interfaces;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Infrastructure.Repositories;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddControllers();
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

// External services

builder.Services.AddSingleton<BlobServiceClient>(provider =>
    new BlobServiceClient(builder.Configuration["Azure:Storage:ConnectionString"]));

// Hangfire for background jobs
builder.Services.AddHangfire(config =>
    config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// HttpClient for external API calls
builder.Services.AddHttpClient<IQuickBooksService, QuickBooksService>();

// Repository pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IShopperAuthService, ShopperAuthService>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ISplitCalculationService, SplitCalculationService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProviderNotificationService, ProviderNotificationService>();
builder.Services.AddScoped<IStatementService, StatementService>();
builder.Services.AddScoped<ISuggestionService, SuggestionService>();
// Report services (focused single-responsibility services)
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IInventoryReportService, InventoryReportService>();
builder.Services.AddScoped<IPayoutReportService, PayoutReportService>();
builder.Services.AddScoped<IProviderReportService, ProviderReportService>();
builder.Services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IStoreCodeService, StoreCodeService>();
builder.Services.AddScoped<ISetupWizardService, SetupWizardService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<StatementGenerationJob>();

// Storefront services
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStripePaymentService, StripePaymentService>();

// Photo storage implementations
builder.Services.AddScoped<CloudinaryPhotoService>();
builder.Services.AddScoped<AzurePhotoService>();
builder.Services.AddScoped<IPhotoService, PhotoService>(); // Factory that switches between implementations

// Payment gateway implementations
builder.Services.AddScoped<StripePaymentGatewayService>();
builder.Services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

// Accounting service implementations
builder.Services.AddScoped<IQuickBooksService, QuickBooksService>();
builder.Services.AddScoped<QuickBooksAccountingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ConsignmentGenie API v1");
    });
    app.UseHangfireDashboard();
}

// Middleware order is important
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Schedule background jobs after Hangfire is initialized
app.MapHangfireDashboard();

// Use a startup filter to schedule jobs after the application starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Schedule monthly statement generation job (runs on 1st of each month at 2 AM)
    recurringJobManager.AddOrUpdate<StatementGenerationJob>(
        "generate-monthly-statements",
        job => job.GenerateMonthlyStatementsAsync(),
        "0 2 1 * *"); // Cron expression: 2 AM on the 1st day of every month
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
