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

public class ConsignorNotificationsControllerTests
{
    private readonly Mock<IConsignorNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<ConsignorNotificationsController>> _mockLogger;
    private readonly ConsignorNotificationsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ConsignorNotificationsControllerTests()
    {
        _mockNotificationService = new Mock<IConsignorNotificationService>();
        _mockLogger = new Mock<ILogger<ConsignorNotificationsController>>();
        _controller = new ConsignorNotificationsController(_mockNotificationService.Object, _mockLogger.Object);

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

        var expectedResult = new PagedResult<NotificationDto>(
            new List<NotificationDto>
            {
                new NotificationDto
                {
                    NotificationId = Guid.NewGuid(),
                    Type = "ItemSold",
                    Title = "Item Sold",
                    Message = "Your item sold",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    TimeAgo = "1 hour ago"
                }
            },
            1, 1, 10);

        _mockNotificationService
            .Setup(x => x.GetNotificationsAsync(_testUserId, queryParams))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetNotifications(queryParams);

        // Assert
        var actionResult = Assert.IsType<ActionResult<PagedResult<NotificationDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedData = Assert.IsType<PagedResult<NotificationDto>>(okResult.Value);
        Assert.Equal(1, returnedData.TotalCount);
        Assert.Equal("Item Sold", returnedData.Data.First().Title);
    }

    [Fact]
    public async Task GetNotifications_WithUnauthorizedUser_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(); // No claims

        var queryParams = new NotificationQueryParams();

        // Act
        var result = await _controller.GetNotifications(queryParams);

        // Assert
        var actionResult = Assert.IsType<ActionResult<PagedResult<NotificationDto>>>(result);
        Assert.IsType<UnauthorizedResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetUnreadCount_WithValidUser_ReturnsCount()
    {
        // Arrange
        const int expectedCount = 5;
        _mockNotificationService
            .Setup(x => x.GetUnreadCountAsync(_testUserId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        var actionResult = Assert.IsType<ActionResult<object>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedData = okResult.Value;

        // Use reflection to get the count property
        var countProperty = returnedData.GetType().GetProperty("count");
        Assert.NotNull(countProperty);
        var actualCount = (int)countProperty.GetValue(returnedData);
        Assert.Equal(expectedCount, actualCount);
    }

    [Fact]
    public async Task MarkAsRead_WithValidNotification_ReturnsNoContent()
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
    public async Task MarkAllAsRead_WithValidUser_ReturnsNoContent()
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
    public async Task DeleteNotification_WithValidNotification_ReturnsNoContent()
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
    public async Task GetPreferences_WithValidUser_ReturnsPreferences()
    {
        // Arrange
        var expectedPreferences = new NotificationPreferencesDto
        {
            EmailEnabled = true,
            EmailItemSold = true,
            EmailPayoutProcessed = true,
            DigestMode = "instant",
            DigestTime = "09:00",
            DigestDay = 1,
            PayoutPendingThreshold = 50.00m
        };

        _mockNotificationService
            .Setup(x => x.GetPreferencesAsync(_testUserId))
            .ReturnsAsync(expectedPreferences);

        // Act
        var result = await _controller.GetPreferences();

        // Assert
        var actionResult = Assert.IsType<ActionResult<NotificationPreferencesDto>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedPreferences = Assert.IsType<NotificationPreferencesDto>(okResult.Value);
        Assert.Equal(expectedPreferences.EmailEnabled, returnedPreferences.EmailEnabled);
        Assert.Equal(expectedPreferences.DigestMode, returnedPreferences.DigestMode);
        Assert.Equal(expectedPreferences.PayoutPendingThreshold, returnedPreferences.PayoutPendingThreshold);
    }

    [Fact]
    public async Task UpdatePreferences_WithValidRequest_ReturnsUpdatedPreferences()
    {
        // Arrange
        var request = new UpdateNotificationPreferencesRequest
        {
            EmailEnabled = false,
            EmailItemSold = false,
            EmailPayoutProcessed = true,
            DigestMode = "daily",
            DigestTime = "10:00",
            DigestDay = 2,
            PayoutPendingThreshold = 100.00m
        };

        var expectedResult = new NotificationPreferencesDto
        {
            EmailEnabled = request.EmailEnabled,
            EmailItemSold = request.EmailItemSold,
            EmailPayoutProcessed = request.EmailPayoutProcessed,
            DigestMode = request.DigestMode,
            DigestTime = request.DigestTime,
            DigestDay = request.DigestDay ?? 1,
            PayoutPendingThreshold = request.PayoutPendingThreshold ?? 50.00m
        };

        _mockNotificationService
            .Setup(x => x.UpdatePreferencesAsync(_testUserId, request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UpdatePreferences(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPreferences = Assert.IsType<NotificationPreferencesDto>(okResult.Value);
        Assert.Equal(expectedResult.EmailEnabled, returnedPreferences.EmailEnabled);
        Assert.Equal(expectedResult.DigestMode, returnedPreferences.DigestMode);
        Assert.Equal(expectedResult.PayoutPendingThreshold, returnedPreferences.PayoutPendingThreshold);
    }

    [Fact]
    public async Task UpdatePreferences_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateNotificationPreferencesRequest(); // Invalid request
        _controller.ModelState.AddModelError("DigestMode", "Required");

        // Act
        var result = await _controller.UpdatePreferences(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task GetNotifications_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var queryParams = new NotificationQueryParams();
        _mockNotificationService
            .Setup(x => x.GetNotificationsAsync(_testUserId, queryParams))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetNotifications(queryParams);

        // Assert
        var actionResult = Assert.IsType<ActionResult<PagedResult<NotificationDto>>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetUnreadCount_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.GetUnreadCountAsync(_testUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUnreadCount();

        // Assert
        var actionResult = Assert.IsType<ActionResult<object>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _mockNotificationService
            .Setup(x => x.MarkAsReadAsync(notificationId, _testUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.MarkAsRead(notificationId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetPreferences_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockNotificationService
            .Setup(x => x.GetPreferencesAsync(_testUserId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPreferences();

        // Assert
        var actionResult = Assert.IsType<ActionResult<NotificationPreferencesDto>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}