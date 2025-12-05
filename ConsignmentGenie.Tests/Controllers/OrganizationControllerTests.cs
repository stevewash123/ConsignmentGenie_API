using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Core.DTOs.Onboarding;
using ConsignmentGenie.Core.DTOs.Settings;
using ConsignmentGenie.Infrastructure.Data;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers
{
    public class OrganizationControllerTests : IDisposable
    {
        private readonly ConsignmentGenieContext _context;
        private readonly OrganizationController _controller;
        private readonly Guid _organizationId = new("11111111-1111-1111-1111-111111111111");

        public OrganizationControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            var mockLogger = new Mock<ILogger<OrganizationController>>();
            _controller = new OrganizationController(_context, mockLogger.Object);

            // Mock JWT claims
            var claims = new[]
            {
                new Claim("organizationId", _organizationId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            var organization = new Organization
            {
                Id = _organizationId,
                Name = "Test Shop",
                StoreCode = "TEST",
                StoreCodeEnabled = true,
                AutoApproveConsignors = true,
                OnboardingDismissed = false,
                WelcomeGuideCompleted = false,
                StoreEnabled = false,
                StripeConnected = false,
                QuickBooksConnected = false,
                VerticalType = VerticalType.Consignment,
                SubscriptionStatus = SubscriptionStatus.Active,
                SubscriptionTier = SubscriptionTier.Pro,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsCorrectSetupStatus_WhenAllStepsIncomplete()
        {
            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            // Serialize and deserialize to work around anonymous type issues
            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());

            var data = response.GetProperty("data");
            Assert.False(data.GetProperty("dismissed").GetBoolean());
            Assert.False(data.GetProperty("welcomeGuideCompleted").GetBoolean());
            Assert.True(data.GetProperty("showModal").GetBoolean()); // Should show modal since steps are incomplete

            var steps = data.GetProperty("steps");
            Assert.False(steps.GetProperty("hasConsignors").GetBoolean());
            Assert.False(steps.GetProperty("storefrontConfigured").GetBoolean());
            Assert.False(steps.GetProperty("hasInventory").GetBoolean());
            Assert.False(steps.GetProperty("quickBooksConnected").GetBoolean());
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsCorrectSetupStatus_WhenSomeStepsComplete()
        {
            // Arrange - Add a consignor and enable store
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.StoreEnabled = true;

            var consignor = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                FirstName = "Test",
                LastName = "Consignor",
                Email = "test@consignor.com",
                Status = ConsignorStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(consignor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());

            var data = response.GetProperty("data");
            Assert.True(data.GetProperty("showModal").GetBoolean()); // Still should show since not all steps complete

            var steps = data.GetProperty("steps");
            Assert.True(steps.GetProperty("hasConsignors").GetBoolean());
            Assert.True(steps.GetProperty("storefrontConfigured").GetBoolean());
            Assert.False(steps.GetProperty("hasInventory").GetBoolean());
            Assert.False(steps.GetProperty("quickBooksConnected").GetBoolean());
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsShowModalFalse_WhenAllStepsComplete()
        {
            // Arrange - Complete all steps
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.StoreEnabled = true;
            organization.QuickBooksConnected = true;

            var consignor = new Consignor
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                FirstName = "Test",
                LastName = "Consignor",
                Email = "test@consignor.com",
                Status = ConsignorStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };
            _context.Consignors.Add(consignor);

            var item = new Item
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ConsignorId = consignor.Id,
                Title = "Test Item",
                Price = 100.00m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Items.Add(item);

            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            var data = response.GetProperty("data");
            Assert.False(data.GetProperty("showModal").GetBoolean()); // Should not show modal since all steps complete

            var steps = data.GetProperty("steps");
            Assert.True(steps.GetProperty("hasConsignors").GetBoolean());
            Assert.True(steps.GetProperty("storefrontConfigured").GetBoolean());
            Assert.True(steps.GetProperty("hasInventory").GetBoolean());
            Assert.True(steps.GetProperty("quickBooksConnected").GetBoolean());
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsShowModalFalse_WhenWelcomeGuideCompleted()
        {
            // Arrange - Complete welcome guide but leave some steps incomplete
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.WelcomeGuideCompleted = true;
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            var data = response.GetProperty("data");
            Assert.True(data.GetProperty("welcomeGuideCompleted").GetBoolean());
            Assert.False(data.GetProperty("showModal").GetBoolean()); // Should not show modal since welcome guide completed
        }

        [Fact]
        public async Task GetSetupStatus_ReturnsNotFound_WhenOrganizationNotExists()
        {
            // Arrange - Remove the organization
            var organization = await _context.Organizations.FindAsync(_organizationId);
            _context.Organizations.Remove(organization!);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetSetupStatus();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task DismissWelcomeGuide_UpdatesWelcomeGuideCompleted()
        {
            // Act
            var result = await _controller.DismissWelcomeGuide();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.True(response.GetProperty("welcomeGuideCompleted").GetBoolean());
            Assert.Equal("Welcome guide dismissed successfully", response.GetProperty("message").GetString());

            // Verify the database was updated
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.True(organization!.WelcomeGuideCompleted);
        }

        [Fact]
        public async Task DismissWelcomeGuide_ReturnsNotFound_WhenOrganizationNotExists()
        {
            // Arrange - Remove the organization
            var organization = await _context.Organizations.FindAsync(_organizationId);
            _context.Organizations.Remove(organization!);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DismissWelcomeGuide();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task DismissWelcomeGuide_UpdatesTimestamp()
        {
            // Arrange - Get initial timestamp
            var organization = await _context.Organizations.FindAsync(_organizationId);
            var initialTimestamp = organization!.UpdatedAt;

            // Wait a small amount to ensure timestamp difference
            await Task.Delay(10);

            // Act
            await _controller.DismissWelcomeGuide();

            // Assert
            var updatedOrganization = await _context.Organizations.FindAsync(_organizationId);
            Assert.True(updatedOrganization!.UpdatedAt > initialTimestamp);
        }

        [Fact]
        public async Task GetBusinessSettings_ReturnsDefaultSettings_WhenNoSettingsStored()
        {
            // Act
            var result = await _controller.GetBusinessSettings();

            // Assert
            var actionResult = Assert.IsType<ActionResult<BusinessSettingsDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var settings = Assert.IsType<BusinessSettingsDto>(okResult.Value);

            Assert.NotNull(settings);
            Assert.NotNull(settings.Commission);
            Assert.NotNull(settings.Tax);
            Assert.NotNull(settings.Payouts);
            Assert.NotNull(settings.Items);

            // Verify default values
            Assert.Equal("60/40", settings.Commission.DefaultSplit);
            Assert.False(settings.Commission.AllowCustomSplitsPerConsignor);
            Assert.False(settings.Commission.AllowCustomSplitsPerItem);
            Assert.Equal(0, settings.Tax.SalesTaxRate);
            Assert.Equal("monthly", settings.Payouts.Schedule);
            Assert.Equal(25.00m, settings.Payouts.MinimumAmount);
            Assert.Equal(14, settings.Payouts.HoldPeriodDays);
            Assert.Equal(90, settings.Items.DefaultConsignmentPeriodDays);
        }

        [Fact]
        public async Task UpdateBusinessSettings_SavesSettingsSuccessfully()
        {
            // Arrange
            var businessSettings = new BusinessSettingsDto
            {
                Commission = new CommissionDto
                {
                    DefaultSplit = "70/30",
                    AllowCustomSplitsPerConsignor = true,
                    AllowCustomSplitsPerItem = false
                },
                Tax = new TaxDto
                {
                    SalesTaxRate = 8.5m,
                    TaxIncludedInPrices = true,
                    ChargeTaxOnShipping = false
                },
                Payouts = new PayoutDto
                {
                    Schedule = "weekly",
                    MinimumAmount = 50.00m,
                    HoldPeriodDays = 7
                },
                Items = new ItemPolicyDto
                {
                    DefaultConsignmentPeriodDays = 60,
                    EnableAutoMarkdowns = true,
                    MarkdownSchedule = new MarkdownScheduleDto
                    {
                        After30Days = 10,
                        After60Days = 20,
                        After90DaysAction = "donate"
                    }
                }
            };

            // Act
            var result = await _controller.UpdateBusinessSettings(businessSettings);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.Equal("Business settings updated successfully", response.GetProperty("message").GetString());

            // Verify the database was updated
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.NotNull(organization!.BusinessSettings);
            Assert.Equal(70m, organization.DefaultSplitPercentage); // Verify basic field sync
            Assert.Equal(0.085m, organization.TaxRate); // Verify tax rate conversion
        }

        [Fact]
        public async Task GetStorefrontSettings_ReturnsDefaultSettings_WhenNoSettingsStored()
        {
            // Act
            var result = await _controller.GetStorefrontSettings();

            // Assert
            var actionResult = Assert.IsType<ActionResult<StorefrontSettingsDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var settings = Assert.IsType<StorefrontSettingsDto>(okResult.Value);

            Assert.NotNull(settings);
            Assert.Equal("cg-storefront", settings.SelectedChannel);
            Assert.NotNull(settings.Square);
            Assert.NotNull(settings.Shopify);
            Assert.NotNull(settings.CgStorefront);
            Assert.NotNull(settings.InStore);

            // Verify default values
            Assert.False(settings.Square.Connected);
            Assert.True(settings.Square.SyncInventory);
            Assert.False(settings.Shopify.Connected);
            Assert.True(settings.Shopify.PushInventory);
            Assert.False(settings.CgStorefront.DnsVerified);
            Assert.Equal("#2563eb", settings.CgStorefront.PrimaryColor);
            Assert.True(settings.InStore.UseReceiptNumbers);
            Assert.Equal(1, settings.InStore.NextReceiptNumber);
        }

        [Fact]
        public async Task UpdateStorefrontSettings_SavesSettingsSuccessfully()
        {
            // Arrange
            var storefrontSettings = new StorefrontSettingsDto
            {
                SelectedChannel = "shopify",
                Square = new SquareSettingsDto
                {
                    Connected = true,
                    BusinessName = "Test Business",
                    SyncInventory = false,
                    ImportSales = true
                },
                Shopify = new ShopifySettingsDto
                {
                    Connected = true,
                    StoreName = "Test Shopify Store",
                    PushInventory = false,
                    ImportOrders = true
                },
                CgStorefront = new CgStorefrontSettingsDto
                {
                    StoreSlug = "test-store",
                    CustomDomain = "test.example.com",
                    DnsVerified = true,
                    StripeConnected = true,
                    PrimaryColor = "#ff0000",
                    AccentColor = "#00ff00"
                },
                InStore = new InStoreSettingsDto
                {
                    UseReceiptNumbers = false,
                    ReceiptPrefix = "CG",
                    NextReceiptNumber = 100,
                    RequireManagerApproval = true
                }
            };

            // Act
            var result = await _controller.UpdateStorefrontSettings(storefrontSettings);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);

            var responseString = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var response = doc.RootElement;

            Assert.True(response.GetProperty("success").GetBoolean());
            Assert.Equal("Storefront settings updated successfully", response.GetProperty("message").GetString());

            // Verify the database was updated
            var organization = await _context.Organizations.FindAsync(_organizationId);
            Assert.NotNull(organization!.StorefrontSettings);
            Assert.Equal("test-store", organization.Slug); // Verify basic field sync
            Assert.True(organization.StripeConnected); // Verify boolean field sync
        }

        [Fact]
        public async Task GetBusinessSettings_ReturnsNotFound_WhenOrganizationNotExists()
        {
            // Arrange - Remove the organization
            var organization = await _context.Organizations.FindAsync(_organizationId);
            _context.Organizations.Remove(organization!);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetBusinessSettings();

            // Assert
            var actionResult = Assert.IsType<ActionResult<BusinessSettingsDto>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetStorefrontSettings_ReturnsNotFound_WhenOrganizationNotExists()
        {
            // Arrange - Remove the organization
            var organization = await _context.Organizations.FindAsync(_organizationId);
            _context.Organizations.Remove(organization!);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetStorefrontSettings();

            // Assert
            var actionResult = Assert.IsType<ActionResult<StorefrontSettingsDto>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}