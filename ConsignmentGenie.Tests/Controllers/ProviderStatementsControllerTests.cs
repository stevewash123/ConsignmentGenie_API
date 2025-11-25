using ConsignmentGenie.API.Controllers;
using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Core.DTOs.Statements;
using ConsignmentGenie.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ConsignmentGenie.Tests.Controllers;

public class ProviderStatementsControllerTests
{
    private readonly Mock<IStatementService> _mockStatementService;
    private readonly Mock<ILogger<ProviderStatementsController>> _mockLogger;
    private readonly ProviderStatementsController _controller;
    private readonly Guid _testProviderId = Guid.NewGuid();

    public ProviderStatementsControllerTests()
    {
        _mockStatementService = new Mock<IStatementService>();
        _mockLogger = new Mock<ILogger<ProviderStatementsController>>();
        _controller = new ProviderStatementsController(_mockStatementService.Object, _mockLogger.Object);

        // Setup the controller's HttpContext with provider claim
        var claims = new List<Claim>
        {
            new Claim("ProviderId", _testProviderId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetStatements_WithValidProvider_ReturnsOkResult()
    {
        // Arrange
        var expectedStatements = new List<StatementListDto>
        {
            new StatementListDto
            {
                StatementId = Guid.NewGuid(),
                StatementNumber = "STMT-2023-11-TEST",
                PeriodStart = new DateTime(2023, 11, 1),
                PeriodEnd = new DateTime(2023, 11, 30),
                PeriodLabel = "November 2023",
                ItemsSold = 5,
                TotalEarnings = 300.00m,
                ClosingBalance = 150.00m,
                Status = "Generated",
                HasPdf = false,
                GeneratedAt = DateTime.UtcNow
            }
        };

        _mockStatementService
            .Setup(x => x.GetStatementsAsync(_testProviderId))
            .ReturnsAsync(expectedStatements);

        // Act
        var result = await _controller.GetStatements();

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<List<StatementListDto>>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<List<StatementListDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("November 2023", response.Data.First().PeriodLabel);
        Assert.Equal(300.00m, response.Data.First().TotalEarnings);
    }

    [Fact]
    public async Task GetStatements_WithoutProviderContext_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            // No ProviderId claim
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = await _controller.GetStatements();

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<List<StatementListDto>>>>(result);
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<List<StatementListDto>>>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Provider context required", response.Errors);
    }

    [Fact]
    public async Task GetStatement_WithValidId_ReturnsStatement()
    {
        // Arrange
        var statementId = Guid.NewGuid();
        var expectedStatement = new StatementDto
        {
            Id = statementId,
            StatementNumber = "STMT-2023-11-TEST",
            PeriodStart = new DateOnly(2023, 11, 1),
            PeriodEnd = new DateOnly(2023, 11, 30),
            PeriodLabel = "November 2023",
            ProviderName = "Test Provider",
            ShopName = "Test Shop",
            TotalSales = 500.00m,
            TotalEarnings = 300.00m,
            TotalPayouts = 200.00m,
            ClosingBalance = 100.00m,
            ItemsSold = 5,
            PayoutCount = 2,
            Sales = new List<StatementSaleLineDto>(),
            Payouts = new List<StatementPayoutLineDto>(),
            Status = "Generated",
            GeneratedAt = DateTime.UtcNow
        };

        _mockStatementService
            .Setup(x => x.GetStatementAsync(statementId, _testProviderId))
            .ReturnsAsync(expectedStatement);

        _mockStatementService
            .Setup(x => x.MarkAsViewedAsync(statementId, _testProviderId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetStatement(statementId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StatementDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(statementId, response.Data.Id);
        Assert.Equal("Test Provider", response.Data.ProviderName);
        Assert.Equal(300.00m, response.Data.TotalEarnings);

        // Verify that MarkAsViewed was called
        _mockStatementService.Verify(x => x.MarkAsViewedAsync(statementId, _testProviderId), Times.Once);
    }

    [Fact]
    public async Task GetStatement_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var statementId = Guid.NewGuid();

        _mockStatementService
            .Setup(x => x.GetStatementAsync(statementId, _testProviderId))
            .ReturnsAsync((StatementDto)null);

        // Act
        var result = await _controller.GetStatement(statementId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StatementDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Statement not found", response.Errors);
    }

    [Fact]
    public async Task GetStatementByPeriod_WithValidPeriod_ReturnsStatement()
    {
        // Arrange
        const int year = 2023;
        const int month = 11;
        var expectedStatement = new StatementDto
        {
            Id = Guid.NewGuid(),
            StatementNumber = "STMT-2023-11-TEST",
            PeriodStart = new DateOnly(year, month, 1),
            PeriodEnd = new DateOnly(year, month, 30),
            PeriodLabel = "November 2023"
        };

        _mockStatementService
            .Setup(x => x.GetStatementByPeriodAsync(
                _testProviderId,
                new DateOnly(year, month, 1),
                new DateOnly(year, month, 30)))
            .ReturnsAsync(expectedStatement);

        _mockStatementService
            .Setup(x => x.MarkAsViewedAsync(expectedStatement.Id, _testProviderId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.GetStatementByPeriod(year, month);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StatementDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("November 2023", response.Data.PeriodLabel);
    }

    [Fact]
    public async Task GetStatementByPeriod_WithInvalidMonth_ReturnsBadRequest()
    {
        // Arrange
        const int year = 2023;
        const int invalidMonth = 13;

        // Act
        var result = await _controller.GetStatementByPeriod(year, invalidMonth);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StatementDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid month", response.Errors);
    }

    [Fact]
    public async Task GetStatementByPeriod_WithNonExistentPeriod_ReturnsNotFound()
    {
        // Arrange
        const int year = 2023;
        const int month = 11;

        _mockStatementService
            .Setup(x => x.GetStatementByPeriodAsync(
                _testProviderId,
                new DateOnly(year, month, 1),
                new DateOnly(year, month, 30)))
            .ReturnsAsync((StatementDto)null);

        // Act
        var result = await _controller.GetStatementByPeriod(year, month);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StatementDto>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Statement not found", response.Errors);
    }

    [Fact]
    public async Task GetStatementPdf_WithValidStatement_ReturnsFile()
    {
        // Arrange
        var statementId = Guid.NewGuid();
        var expectedStatement = new StatementDto
        {
            Id = statementId,
            StatementNumber = "STMT-2023-11-TEST"
        };

        var pdfContent = System.Text.Encoding.UTF8.GetBytes("PDF content");

        _mockStatementService
            .Setup(x => x.GetStatementAsync(statementId, _testProviderId))
            .ReturnsAsync(expectedStatement);

        _mockStatementService
            .Setup(x => x.GeneratePdfAsync(statementId, _testProviderId))
            .ReturnsAsync(pdfContent);

        // Act
        var result = await _controller.GetStatementPdf(statementId);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("STMT-2023-11-TEST.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task GetStatementPdf_WithNonExistentStatement_ReturnsNotFound()
    {
        // Arrange
        var statementId = Guid.NewGuid();

        _mockStatementService
            .Setup(x => x.GetStatementAsync(statementId, _testProviderId))
            .ReturnsAsync((StatementDto)null);

        // Act
        var result = await _controller.GetStatementPdf(statementId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetStatementPdfByPeriod_WithValidPeriod_ReturnsFile()
    {
        // Arrange
        const int year = 2023;
        const int month = 11;
        var expectedStatement = new StatementDto
        {
            Id = Guid.NewGuid(),
            StatementNumber = "STMT-2023-11-TEST"
        };

        var pdfContent = System.Text.Encoding.UTF8.GetBytes("PDF content");

        _mockStatementService
            .Setup(x => x.GetStatementByPeriodAsync(
                _testProviderId,
                new DateOnly(year, month, 1),
                new DateOnly(year, month, 30)))
            .ReturnsAsync(expectedStatement);

        _mockStatementService
            .Setup(x => x.GeneratePdfAsync(expectedStatement.Id, _testProviderId))
            .ReturnsAsync(pdfContent);

        // Act
        var result = await _controller.GetStatementPdfByPeriod(year, month);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", fileResult.ContentType);
        Assert.Equal("STMT-2023-11-TEST.pdf", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task RegenerateStatement_WithValidId_ReturnsUpdatedStatement()
    {
        // Arrange
        var statementId = Guid.NewGuid();
        var regeneratedStatement = new StatementDto
        {
            Id = Guid.NewGuid(), // New ID for regenerated statement
            StatementNumber = "STMT-2023-11-REGEN",
            TotalEarnings = 400.00m, // Updated amount
            ItemsSold = 6
        };

        _mockStatementService
            .Setup(x => x.RegenerateStatementAsync(statementId, _testProviderId))
            .ReturnsAsync(regeneratedStatement);

        // Act
        var result = await _controller.RegenerateStatement(statementId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse<StatementDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(400.00m, response.Data.TotalEarnings);
        Assert.Equal(6, response.Data.ItemsSold);
    }

    [Fact]
    public async Task GetStatements_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockStatementService
            .Setup(x => x.GetStatementsAsync(_testProviderId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetStatements();

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<List<StatementListDto>>>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<List<StatementListDto>>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Contains("An error occurred while retrieving statements", response.Errors);
    }

    [Fact]
    public async Task GetStatement_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var statementId = Guid.NewGuid();
        _mockStatementService
            .Setup(x => x.GetStatementAsync(statementId, _testProviderId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetStatement(statementId);

        // Assert
        var actionResult = Assert.IsType<ActionResult<ApiResponse<StatementDto>>>(result);
        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(500, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<StatementDto>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Contains("An error occurred while retrieving statement", response.Errors);
    }

    [Fact]
    public async Task GetStatementPdf_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var statementId = Guid.NewGuid();
        _mockStatementService
            .Setup(x => x.GetStatementAsync(statementId, _testProviderId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetStatementPdf(statementId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}