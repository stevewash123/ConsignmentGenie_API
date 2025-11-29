using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Core.DTOs;
using ConsignmentGenie.Core.DTOs.Notifications;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers;

public class OwnerControllerTests
{
    private readonly Mock<IProviderNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<OwnerController>> _mockLogger;
    private readonly OwnerController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public OwnerControllerTests()
    {
        _mockNotificationService = new Mock<IProviderNotificationService>();
        _mockLogger = new Mock<ILogger<OwnerController>>();
        _controller = new OwnerController(_mockNotificationService.Object, _mockLogger.Object);

        // Setup the controller's HttpContext with a user claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetNotifications_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var queryParams = new NotificationQueryParams
        {
            Page = 1,
            PageSize = 10,
            UnreadOnly = false
        };

        var expectedResult = new PagedResult<NotificationDto>
        {
            Data = new List<NotificationDto>
            {
                new NotificationDto
                {
                    NotificationId = Guid.NewGuid(),
                    Title = "Test Notification",
                    Message = "Test message",
                    Type = "provider_request",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10,
            TotalPages = 1
        };

        _mockNotificationService
            .Setup(x => x.GetNotificationsAsync(_testUserId, queryParams))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetNotifications(queryParams);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<PagedResult<NotificationDto>>(okResult.Value);
        Assert.Equal(expectedResult.TotalCount, returnedResult.TotalCount);
        Assert.Equal(expectedResult.Data.Count, returnedResult.Data.Count);
        _mockNotificationService.Verify(x => x.GetNotificationsAsync(_testUserId, queryParams), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCount_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var expectedCount = 5;
        _mockNotificationService
            .Setup(x => x.GetUnreadCountAsync(_testUserId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedValue = okResult.Value;
        Assert.NotNull(returnedValue);

        // Check if the anonymous object has the expected count property
        var countProperty = returnedValue.GetType().GetProperty("count");
        Assert.NotNull(countProperty);
        Assert.Equal(expectedCount, countProperty.GetValue(returnedValue));

        _mockNotificationService.Verify(x => x.GetUnreadCountAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.MarkAsReadAsync(notificationId, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockNotificationService.Verify(x => x.MarkAsReadAsync(notificationId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.MarkAllAsReadAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.MarkAllAsRead();

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockNotificationService.Verify(x => x.MarkAllAsReadAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteNotification_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.DeleteAsync(notificationId, _testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockNotificationService.Verify(x => x.DeleteAsync(notificationId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task GetPreferences_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var expectedPreferences = new NotificationPreferencesDto
        {
            EmailEnabled = true,
            EmailItemSold = true,
            EmailPayoutProcessed = true,
            EmailStatementReady = true,
            DigestMode = "daily"
        };

        _mockNotificationService
            .Setup(x => x.GetPreferencesAsync(_testUserId))
            .ReturnsAsync(expectedPreferences);

        // Act
        var result = await _controller.GetPreferences();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPreferences = Assert.IsType<NotificationPreferencesDto>(okResult.Value);
        Assert.Equal(expectedPreferences.EmailEnabled, returnedPreferences.EmailEnabled);
        Assert.Equal(expectedPreferences.DigestMode, returnedPreferences.DigestMode);
        _mockNotificationService.Verify(x => x.GetPreferencesAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferences_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new UpdateNotificationPreferencesRequest
        {
            EmailEnabled = true,
            EmailItemSold = true,
            EmailPayoutProcessed = false,
            DigestMode = "weekly"
        };

        var expectedPreferences = new NotificationPreferencesDto
        {
            EmailEnabled = request.EmailEnabled,
            EmailItemSold = request.EmailItemSold,
            EmailPayoutProcessed = request.EmailPayoutProcessed,
            DigestMode = request.DigestMode
        };

        _mockNotificationService
            .Setup(x => x.UpdatePreferencesAsync(_testUserId, request))
            .ReturnsAsync(expectedPreferences);

        // Act
        var result = await _controller.UpdatePreferences(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPreferences = Assert.IsType<NotificationPreferencesDto>(okResult.Value);
        Assert.Equal(expectedPreferences.EmailEnabled, returnedPreferences.EmailEnabled);
        Assert.Equal(expectedPreferences.DigestMode, returnedPreferences.DigestMode);
        _mockNotificationService.Verify(x => x.UpdatePreferencesAsync(_testUserId, request), Times.Once);
    }

    [Fact]
    public async Task GetNotifications_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controllerWithoutClaims = new OwnerController(_mockNotificationService.Object, _mockLogger.Object);
        controllerWithoutClaims.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var queryParams = new NotificationQueryParams();

        // Act
        var result = await controllerWithoutClaims.GetNotifications(queryParams);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _mockNotificationService.Verify(x => x.GetNotificationsAsync(It.IsAny<Guid>(), It.IsAny<NotificationQueryParams>()), Times.Never);
    }

    [Fact]
    public async Task GetUnreadCount_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controllerWithoutClaims = new OwnerController(_mockNotificationService.Object, _mockLogger.Object);
        controllerWithoutClaims.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await controllerWithoutClaims.GetUnreadCount();

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
        _mockNotificationService.Verify(x => x.GetUnreadCountAsync(It.IsAny<Guid>()), Times.Never);
    }
}