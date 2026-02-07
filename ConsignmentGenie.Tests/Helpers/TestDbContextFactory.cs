using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConsignmentGenie.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ConsignmentGenieContext CreateInMemoryContext(string databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new ConsignmentGenieContext(options);
    }

    public static async Task<ConsignmentGenieContext> CreateContextWithDataAsync()
    {
        var context = CreateInMemoryContext();

        // Seed test data
        var organization = new ConsignmentGenie.Core.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            VerticalType = ConsignmentGenie.Core.Enums.VerticalType.Consignment,
            SubscriptionStatus = ConsignmentGenie.Core.Enums.SubscriptionStatus.Active,
            SubscriptionTier = ConsignmentGenie.Core.Enums.SubscriptionTier.Basic
        };

        var user = new ConsignmentGenie.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = ConsignmentGenie.Core.Enums.UserRole.Owner,
            OrganizationId = organization.Id
        };

        var provider = new ConsignmentGenie.Core.Entities.Consignor
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            DisplayName = "Test Consignor",
            Email = "provider@example.com",
            DefaultSplitPercentage = 50.00m,
            Status = ConsignmentGenie.Core.Enums.ConsignorStatus.Active
        };

        context.Organizations.Add(organization);
        context.Users.Add(user);
        context.Consignors.Add(provider);

        await context.SaveChangesAsync();

        return context;
    }
}