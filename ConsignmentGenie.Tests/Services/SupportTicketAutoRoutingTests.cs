using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ConsignmentGenie.Tests.Services;

public class SupportTicketAutoRoutingTests : IDisposable
{
    private readonly ConsignmentGenieContext _context;

    public SupportTicketAutoRoutingTests()
    {
        var options = new DbContextOptionsBuilder<ConsignmentGenieContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ConsignmentGenieContext(options);
    }

    [Fact]
    public async Task SupportTicket_ContentCategory_RoutesToOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var supportTicket = new SupportTicket
        {
            Category = SupportTicketCategory.Content,
            Subject = "Content Issue",
            Description = "There's an issue with content",
            SubmittedById = userId
            // Note: AssignedTo defaults to NotAssigned to test auto-routing
        };

        // Act
        _context.SupportTickets.Add(supportTicket);
        await _context.SaveChangesAsync();

        // Assert
        var savedTicket = await _context.SupportTickets.FirstOrDefaultAsync(st => st.Id == supportTicket.Id);
        Assert.NotNull(savedTicket);
        Assert.Equal(SupportTicketAssignedTo.Owner, savedTicket.AssignedTo);
    }

    [Theory]
    [InlineData(SupportTicketCategory.Technical)]
    [InlineData(SupportTicketCategory.Billing)]
    [InlineData(SupportTicketCategory.Other)]
    public async Task SupportTicket_NonContentCategories_RouteToAdmin(SupportTicketCategory category)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var supportTicket = new SupportTicket
        {
            Category = category,
            Subject = "Technical Issue",
            Description = "There's a technical problem",
            SubmittedById = userId
            // Note: AssignedTo defaults to NotAssigned to test auto-routing
        };

        // Act
        _context.SupportTickets.Add(supportTicket);
        await _context.SaveChangesAsync();

        // Assert
        var savedTicket = await _context.SupportTickets.FirstOrDefaultAsync(st => st.Id == supportTicket.Id);
        Assert.NotNull(savedTicket);
        Assert.Equal(SupportTicketAssignedTo.Admin, savedTicket.AssignedTo);
    }

    [Fact]
    public async Task SupportTicket_ExistingTicketUpdate_DoesNotChangeAssignment()
    {
        // Arrange - Create and save a ticket first
        var userId = Guid.NewGuid();
        var supportTicket = new SupportTicket
        {
            Category = SupportTicketCategory.Content,
            Subject = "Content Issue",
            Description = "There's an issue with content",
            SubmittedById = userId
            // Note: AssignedTo defaults to NotAssigned, will be auto-routed
        };

        _context.SupportTickets.Add(supportTicket);
        await _context.SaveChangesAsync();

        // Verify initial auto-routing worked
        var savedTicket = await _context.SupportTickets.FirstOrDefaultAsync(st => st.Id == supportTicket.Id);
        Assert.Equal(SupportTicketAssignedTo.Owner, savedTicket.AssignedTo);

        // Act - Update the ticket's category and save
        savedTicket.Category = SupportTicketCategory.Technical; // This should route to Admin if it were a new ticket
        savedTicket.Description = "Updated description";
        await _context.SaveChangesAsync();

        // Assert - Assignment should NOT change for existing tickets
        var updatedTicket = await _context.SupportTickets.FirstOrDefaultAsync(st => st.Id == supportTicket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(SupportTicketAssignedTo.Owner, updatedTicket.AssignedTo); // Should remain Owner, not change to Admin
        Assert.Equal("Updated description", updatedTicket.Description);
    }

    [Fact]
    public async Task SupportTicket_ManualAssignment_NotOverriddenByAutoRouting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var supportTicket = new SupportTicket
        {
            Category = SupportTicketCategory.Content, // Would normally route to Owner
            Subject = "Content Issue",
            Description = "There's an issue with content",
            SubmittedById = userId,
            AssignedTo = SupportTicketAssignedTo.Admin // Manually assigned to Admin
        };

        // Act
        _context.SupportTickets.Add(supportTicket);
        await _context.SaveChangesAsync();

        // Assert - Manual assignment should take precedence over auto-routing
        var savedTicket = await _context.SupportTickets.FirstOrDefaultAsync(st => st.Id == supportTicket.Id);
        Assert.NotNull(savedTicket);
        Assert.Equal(SupportTicketAssignedTo.Admin, savedTicket.AssignedTo);
    }

    [Fact]
    public async Task SupportTicket_MultipleTickets_AutoRouteCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tickets = new[]
        {
            new SupportTicket
            {
                Category = SupportTicketCategory.Content,
                Subject = "Content 1",
                Description = "Content issue 1",
                SubmittedById = userId
            },
            new SupportTicket
            {
                Category = SupportTicketCategory.Technical,
                Subject = "Tech 1",
                Description = "Technical issue 1",
                SubmittedById = userId
            },
            new SupportTicket
            {
                Category = SupportTicketCategory.Billing,
                Subject = "Billing 1",
                Description = "Billing issue 1",
                SubmittedById = userId
            }
        };

        // Act
        _context.SupportTickets.AddRange(tickets);
        await _context.SaveChangesAsync();

        // Assert
        var savedTickets = await _context.SupportTickets.ToListAsync();

        var contentTicket = savedTickets.First(t => t.Category == SupportTicketCategory.Content);
        var techTicket = savedTickets.First(t => t.Category == SupportTicketCategory.Technical);
        var billingTicket = savedTickets.First(t => t.Category == SupportTicketCategory.Billing);

        Assert.Equal(SupportTicketAssignedTo.Owner, contentTicket.AssignedTo);
        Assert.Equal(SupportTicketAssignedTo.Admin, techTicket.AssignedTo);
        Assert.Equal(SupportTicketAssignedTo.Admin, billingTicket.AssignedTo);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}