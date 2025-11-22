# Service Interface Architecture

## Overview

This architecture implements a granular service interface pattern with automatic fallback support for third-party integrations. Each major service category is broken down into fine-grained interfaces that support multiple implementations with seamless fallback capabilities.

## Service Categories

### 🧾 Accounting Services
**Primary:** QuickBooks API
**Fallback:** Spreadsheet Export

- **`IAccountingInvoices`** - Invoice management operations
- **`IAccountingPayments`** - Payment processing and tracking
- **`IAccountingReports`** - Financial reporting and analytics

### 🛒 Storefront Services
**Primary:** Square/Shopify API
**Fallback:** Internal Store Module

- **`IStorefrontCatalog`** - Product catalog management
- **`IStorefrontOrders`** - Order processing and management
- **`IStorefrontAnalytics`** - Traffic and conversion metrics
- **`IStorefrontConfiguration`** - Store settings and customization

### 💳 Payment Services
**Primary:** Stripe API
**Limited Fallback:** Internal (cash/check only)

- **`IPaymentProcessor`** - Payment processing operations
- **`IPaymentAnalytics`** - Payment reporting and analytics
- **`IPaymentConfiguration`** - Payment method configuration

## Architecture Benefits

### 1. **Granular Interfaces**
```csharp
// Instead of one monolithic interface:
// IAccountingService { CreateInvoice(), RecordPayment(), GenerateReport() }

// We have specialized interfaces:
IAccountingInvoices invoiceService = serviceFactory.GetAccountingInvoicesService();
IAccountingPayments paymentService = serviceFactory.GetAccountingPaymentsService();
IAccountingReports reportService = serviceFactory.GetAccountingReportsService();
```

### 2. **Flexible Fallback Options**
```csharp
// Can mix and match primary and fallback services:
var invoices = serviceFactory.GetAccountingInvoicesService(useFallback: false); // QuickBooks
var payments = serviceFactory.GetAccountingPaymentsService(useFallback: true);  // Spreadsheet
```

### 3. **Automatic Fallback with Decorator Pattern**
```csharp
public class AccountingInvoicesWithFallback : IAccountingInvoices
{
    private readonly IServiceWithFallback<IAccountingInvoices> _serviceWithFallback;

    public async Task<string> CreateInvoiceAsync(InvoiceDto invoice)
    {
        return await _serviceWithFallback.ExecuteWithFallbackAsync(async service =>
            await service.CreateInvoiceAsync(invoice));
    }
}
```

### 4. **Service Health Monitoring**
```csharp
var healthStatus = await serviceFactory.GetServiceHealthStatusAsync();

if (!healthStatus.AllPrimaryServicesHealthy)
{
    // Automatically switch to fallbacks or alert administrators
    logger.LogWarning("Primary services degraded, falling back to secondary implementations");
}
```

## Implementation Examples

### QuickBooks → Spreadsheet Fallback
```csharp
// Primary: QuickBooksInvoiceService
public class QuickBooksInvoiceService : IAccountingInvoices
{
    public async Task<string> CreateInvoiceAsync(InvoiceDto invoice)
    {
        // Call QuickBooks API
        var qbInvoice = await _quickBooksClient.CreateInvoiceAsync(invoice.ToQuickBooksInvoice());
        return qbInvoice.Id;
    }
}

// Fallback: SpreadsheetInvoiceService
public class SpreadsheetInvoiceService : IAccountingInvoices
{
    public async Task<string> CreateInvoiceAsync(InvoiceDto invoice)
    {
        // Generate invoice in spreadsheet format
        var invoiceId = Guid.NewGuid().ToString();
        await _spreadsheetService.AppendRowAsync("Invoices", invoice.ToSpreadsheetRow());
        return invoiceId;
    }
}
```

### Square → Internal Store Fallback
```csharp
// Primary: SquareCatalogService
public class SquareCatalogService : IStorefrontCatalog
{
    public async Task PublishProductsAsync(IEnumerable<ProductDto> products)
    {
        // Sync products to Square Online store
        await _squareClient.UpsertCatalogObjectsAsync(products.ToSquareObjects());
    }

    public string GetStorefrontUrl() => $"https://{_squareStoreUrl}";
}

// Fallback: InternalCatalogService
public class InternalCatalogService : IStorefrontCatalog
{
    public async Task PublishProductsAsync(IEnumerable<ProductDto> products)
    {
        // Update internal store database
        await _productRepository.UpsertProductsAsync(products);
    }

    public string GetStorefrontUrl() => $"https://{_domain}/store/{_organizationSlug}";
}
```

## Configuration

### Service Factory Registration (DI)
```csharp
// Program.cs or Startup.cs
services.AddTransient<IAccountingInvoices, QuickBooksInvoiceService>();
services.AddTransient<IAccountingInvoices, SpreadsheetInvoiceService>(); // Fallback

services.AddTransient<IStorefrontCatalog, SquareCatalogService>();
services.AddTransient<IStorefrontCatalog, InternalCatalogService>(); // Fallback

services.AddTransient<IPaymentProcessor, StripePaymentService>();
services.AddTransient<IPaymentProcessor, InternalPaymentService>(); // Limited fallback

services.AddScoped<IServiceFactory, ServiceFactory>();
```

### Service Health Check Configuration
```csharp
services.AddHealthChecks()
    .AddCheck<QuickBooksHealthCheck>("quickbooks")
    .AddCheck<SquareHealthCheck>("square")
    .AddCheck<StripeHealthCheck>("stripe");
```

### Usage in Controllers
```csharp
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IServiceFactory _serviceFactory;

    public InvoicesController(IServiceFactory serviceFactory)
    {
        _serviceFactory = serviceFactory;
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice(InvoiceDto invoice)
    {
        var invoiceService = _serviceFactory.GetAccountingInvoicesService();
        var invoiceId = await invoiceService.CreateInvoiceAsync(invoice);
        return Ok(new { InvoiceId = invoiceId });
    }
}
```

## Testing Strategy

### Mock Individual Interfaces
```csharp
[Test]
public async Task CreateInvoice_WithQuickBooks_ReturnsInvoiceId()
{
    // Arrange
    var mockInvoiceService = new Mock<IAccountingInvoices>();
    mockInvoiceService.Setup(x => x.CreateInvoiceAsync(It.IsAny<InvoiceDto>()))
                      .ReturnsAsync("QB-12345");

    // Act & Assert
    var result = await mockInvoiceService.Object.CreateInvoiceAsync(testInvoice);
    Assert.Equal("QB-12345", result);
}
```

### Test Fallback Scenarios
```csharp
[Test]
public async Task CreateInvoice_QuickBooksDown_FallsBackToSpreadsheet()
{
    // Arrange
    var primaryService = new Mock<IAccountingInvoices>();
    primaryService.Setup(x => x.CreateInvoiceAsync(It.IsAny<InvoiceDto>()))
                  .ThrowsAsync(new QuickBooksConnectionException());

    var fallbackService = new Mock<IAccountingInvoices>();
    fallbackService.Setup(x => x.CreateInvoiceAsync(It.IsAny<InvoiceDto>()))
                   .ReturnsAsync("SHEET-67890");

    var serviceWithFallback = new ServiceWithFallback<IAccountingInvoices>(
        primaryService.Object, fallbackService.Object);

    // Act
    var result = await serviceWithFallback.ExecuteWithFallbackAsync(
        service => service.CreateInvoiceAsync(testInvoice));

    // Assert
    Assert.Equal("SHEET-67890", result);
}
```

## Monitoring and Alerting

The service factory includes built-in health monitoring that can trigger alerts when primary services fail:

```csharp
// Background service for monitoring
public class ServiceHealthMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var health = await _serviceFactory.GetServiceHealthStatusAsync();

            if (!health.AllPrimaryServicesHealthy)
            {
                await _alertService.SendAdminAlertAsync(
                    "Service Degradation",
                    $"Primary services failing: {string.Join(", ", health.ServiceStatuses.Where(s => !s.Value).Select(s => s.Key))}");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

This architecture provides a robust, testable, and maintainable foundation for handling third-party service integrations with graceful degradation capabilities.