using ConsignmentGenie.Application.Attributes;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ConsignmentGenie.Tests.Attributes;

public class RequiresTierAttributeTests : IDisposable
{
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;

    public RequiresTierAttributeTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    [Fact]
    public void OnAuthorization_BasicTierRequiredWithBasicSubscription_AllowsAccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Org",
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };
        _context.Organizations.Add(organization);
        _context.SaveChanges();

        var attribute = new RequiresTierAttribute(SubscriptionTier.Basic);
        var context = CreateAuthorizationFilterContext(organizationId.ToString());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_ProTierRequiredWithBasicSubscription_DeniesAccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Org",
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };
        _context.Organizations.Add(organization);
        _context.SaveChanges();

        var attribute = new RequiresTierAttribute(SubscriptionTier.Pro);
        var context = CreateAuthorizationFilterContext(organizationId.ToString());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(402, objectResult.StatusCode); // Payment Required
    }

    [Fact]
    public void OnAuthorization_ExpiredSubscription_DeniesAccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Org",
            SubscriptionStatus = SubscriptionStatus.Cancelled,
            SubscriptionTier = SubscriptionTier.Pro
        };
        _context.Organizations.Add(organization);
        _context.SaveChanges();

        var attribute = new RequiresTierAttribute(SubscriptionTier.Basic);
        var context = CreateAuthorizationFilterContext(organizationId.ToString());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var objectResult = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(402, objectResult.StatusCode); // Payment Required
    }

    [Fact]
    public void OnAuthorization_TrialSubscription_AllowsAccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Org",
            SubscriptionStatus = SubscriptionStatus.Trial,
            SubscriptionTier = SubscriptionTier.Basic
        };
        _context.Organizations.Add(organization);
        _context.SaveChanges();

        var attribute = new RequiresTierAttribute(SubscriptionTier.Basic);
        var context = CreateAuthorizationFilterContext(organizationId.ToString());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_ProTierRequiredWithEnterpriseSubscription_AllowsAccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = organizationId,
            Name = "Test Org",
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Enterprise
        };
        _context.Organizations.Add(organization);
        _context.SaveChanges();

        var attribute = new RequiresTierAttribute(SubscriptionTier.Pro);
        var context = CreateAuthorizationFilterContext(organizationId.ToString());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_InvalidOrganizationId_DeniesAccess()
    {
        // Arrange
        var attribute = new RequiresTierAttribute(SubscriptionTier.Basic);
        var context = CreateAuthorizationFilterContext("invalid-guid");

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_NonExistentOrganization_DeniesAccess()
    {
        // Arrange
        var attribute = new RequiresTierAttribute(SubscriptionTier.Basic);
        var context = CreateAuthorizationFilterContext(Guid.NewGuid().ToString());

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    private AuthorizationFilterContext CreateAuthorizationFilterContext(string organizationId)
    {
        var httpContext = new DefaultHttpContext();

        // Setup service provider with database context
        var services = new ServiceCollection();
        services.AddSingleton(_context);
        httpContext.RequestServices = services.BuildServiceProvider();

        // Setup user claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Role, "ShopOwner"),
            new("OrganizationId", organizationId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}