using ConsignmentGenie.Application.Services;
using ConsignmentGenie.Application.Services.Interfaces;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Entities;
using ConsignmentGenie.Core.Enums;
using ConsignmentGenie.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Text.Json;

namespace ConsignmentGenie.Tests.Services;

public class ProviderNotificationServiceTests : IDisposable
{
    private readonly ProviderNotificationService _service;
    private readonly Infrastructure.Data.ConsignmentGenieContext _context;
    private readonly Mock<INotificationService> _mockEmailService;
    private readonly Mock<ILogger<ProviderNotificationService>> _mockLogger;

    public ProviderNotificationServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _mockEmailService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<ProviderNotificationService>>();

        _service = new ProviderNotificationService(
            _context,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateNotificationAsync_ValidRequest_CreatesNotification()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.Provider,
            OrganizationId = organization.Id
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            ProviderNumber = "PRV-001",
            FirstName = "Test",
            LastName = "Provider",
            Email = "provider@test.com",
            CommissionRate = 60.00m,
            Status = ProviderStatus.Approved
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        var request = new CreateNotificationRequest
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            ProviderId = provider.Id,
            Type = NotificationType.ItemSold,
            Title = "Test Notification",
            Message = "Test message",
            RelatedEntityType = "Item",
            RelatedEntityId = Guid.NewGuid(),
            Metadata = new NotificationMetadata
            {
                ItemTitle = "Test Item",
                SalePrice = 100.00m,
                EarningsAmount = 60.00m
            }
        };

        // Act
        await _service.CreateNotificationAsync(request);

        // Assert
        var notification = await _context.Notifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal(request.OrganizationId, notification.OrganizationId);
        Assert.Equal(request.UserId, notification.UserId);
        Assert.Equal(request.ProviderId, notification.ProviderId);
        Assert.Equal(request.Type.ToString(), notification.Type);
        Assert.Equal(request.Title, notification.Title);
        Assert.Equal(request.Message, notification.Message);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task CreateNotificationAsync_WithEmailPreference_SendsEmail()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Shop",
            VerticalType = VerticalType.Consignment,
            SubscriptionStatus = SubscriptionStatus.Active,
            SubscriptionTier = SubscriptionTier.Basic
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "provider@test.com",
            PasswordHash = "hashedpassword",
            Role = UserRole.Provider,
            OrganizationId = organization.Id
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            ProviderNumber = "PRV-001",
            FirstName = "Test",
            LastName = "Provider",
            Email = "provider@test.com",
            CommissionRate = 60.00m,
            Status = ProviderStatus.Approved
        };

        var preference = new UserNotificationPreference
        {
            UserId = user.Id,
            NotificationType = NotificationType.ItemSold,
            EmailEnabled = true
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        _context.Providers.Add(provider);
        _context.UserNotificationPreferences.Add(preference);
        await _context.SaveChangesAsync();

        _mockEmailService.Setup(x => x.SendAsync(It.IsAny<Application.Models.Notifications.NotificationRequest>()))
                         .ReturnsAsync(true);

        var request = new CreateNotificationRequest
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            ProviderId = provider.Id,
            Type = NotificationType.ItemSold,
            Title = "Test Notification",
            Message = "Test message"
        };

        // Act
        await _service.CreateNotificationAsync(request);

        // Assert
        _mockEmailService.Verify(x => x.SendAsync(It.IsAny<Application.Models.Notifications.NotificationRequest>()),
                                Times.Once);

        var notification = await _context.Notifications.FirstOrDefaultAsync();
        Assert.True(notification.EmailSent);
        Assert.NotNull(notification.EmailSentAt);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithValidUserId_ReturnsPagedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification
            {
                UserId = userId,
                Type = "ItemSold",
                Title = "Item Sold",
                Message = "Your item sold",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Notification
            {
                UserId = userId,
                Type = "PayoutProcessed",
                Title = "Payout Processed",
                Message = "Payout completed",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        var queryParams = new NotificationQueryParams
        {
            Page = 1,
            PageSize = 10,
            UnreadOnly = false
        };

        // Act
        var result = await _service.GetNotificationsAsync(userId, queryParams);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithUnreadOnlyFilter_ReturnsOnlyUnread()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification
            {
                UserId = userId,
                Type = "ItemSold",
                Title = "Item Sold",
                Message = "Your item sold",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new Notification
            {
                UserId = userId,
                Type = "PayoutProcessed",
                Title = "Payout Processed",
                Message = "Payout completed",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        var queryParams = new NotificationQueryParams
        {
            Page = 1,
            PageSize = 10,
            UnreadOnly = true
        };

        // Act
        var result = await _service.GetNotificationsAsync(userId, queryParams);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Data.Count);
        Assert.False(result.Data.First().IsRead);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithUnreadNotifications_ReturnsCorrectCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification { UserId = userId, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = userId, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = userId, IsRead = true, CreatedAt = DateTime.UtcNow }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        var count = await _service.GetUnreadCountAsync(userId);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotification_MarksAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new Notification
        {
            UserId = userId,
            Type = "ItemSold",
            Title = "Test",
            Message = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        await _service.MarkAsReadAsync(notification.Id, userId);

        // Assert
        var updatedNotification = await _context.Notifications.FindAsync(notification.Id);
        Assert.True(updatedNotification.IsRead);
        Assert.NotNull(updatedNotification.ReadAt);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithMultipleUnread_MarksAllAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification { UserId = userId, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = userId, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { UserId = userId, IsRead = true, CreatedAt = DateTime.UtcNow }
        };

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        // Act
        await _service.MarkAllAsReadAsync(userId);

        // Assert
        var allNotifications = await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
        Assert.All(allNotifications, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task DeleteAsync_WithValidNotification_RemovesNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new Notification
        {
            UserId = userId,
            Type = "ItemSold",
            Title = "Test",
            Message = "Test",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(notification.Id, userId);

        // Assert
        var deletedNotification = await _context.Notifications.FindAsync(notification.Id);
        Assert.Null(deletedNotification);
    }

    [Fact]
    public async Task GetPreferencesAsync_WithNoExistingPreferences_ReturnsDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var preferences = await _service.GetPreferencesAsync(userId);

        // Assert
        Assert.True(preferences.EmailEnabled);
        Assert.True(preferences.EmailItemSold);
        Assert.True(preferences.EmailPayoutProcessed);
        Assert.False(preferences.EmailPayoutPending);
        Assert.False(preferences.EmailItemExpired);
        Assert.True(preferences.EmailStatementReady);
        Assert.Equal("instant", preferences.DigestMode);
        Assert.Equal("09:00", preferences.DigestTime);
        Assert.Equal(1, preferences.DigestDay);
        Assert.Equal(50.00m, preferences.PayoutPendingThreshold);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithValidRequest_UpdatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = UserRole.Provider
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new UpdateNotificationPreferencesRequest
        {
            EmailEnabled = false,
            EmailItemSold = false,
            EmailPayoutProcessed = true,
            EmailPayoutPending = true,
            EmailItemExpired = true,
            EmailStatementReady = false,
            EmailAccountUpdate = false,
            DigestMode = "daily",
            DigestTime = "10:30",
            DigestDay = 2,
            PayoutPendingThreshold = 100.00m
        };

        // Act
        var result = await _service.UpdatePreferencesAsync(userId, request);

        // Assert
        Assert.False(result.EmailEnabled);
        Assert.False(result.EmailItemSold);
        Assert.True(result.EmailPayoutProcessed);
        Assert.True(result.EmailPayoutPending);
        Assert.True(result.EmailItemExpired);
        Assert.False(result.EmailStatementReady);
        Assert.False(result.EmailAccountUpdate);
        Assert.Equal("daily", result.DigestMode);
        Assert.Equal("10:30", result.DigestTime);
        Assert.Equal(2, result.DigestDay);
        Assert.Equal(100.00m, result.PayoutPendingThreshold);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}