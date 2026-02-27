using ConsignmentGenie.Application.DTOs.Reservation;
using ConsignmentGenie.Application.Interfaces;
using ConsignmentGenie.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        IReservationService reservationService,
        ILogger<ReservationsController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    private Guid GetOrganizationId() => Guid.Parse(User.FindFirst("OrganizationId")?.Value ?? "");
    private Guid? GetUserId() => Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "", out var id) ? id : null;

    /// <summary>
    /// Get all reservations for the organization with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] string? customerPhone = null)
    {
        var result = await _reservationService.GetReservationsAsync(
            GetOrganizationId(), page, pageSize, status, customerPhone);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific reservation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReservation(Guid id)
    {
        var result = await _reservationService.GetReservationAsync(id, GetOrganizationId());

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new reservation (staff-facing)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        var result = await _reservationService.CreateReservationAsync(
            request, GetOrganizationId(), GetUserId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Update reservation status
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateReservationStatus(Guid id, [FromBody] UpdateReservationStatusRequest request)
    {
        request.ReservationId = id;
        var result = await _reservationService.UpdateReservationStatusAsync(
            request, GetOrganizationId(), GetUserId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Send verification code to customer
    /// </summary>
    [HttpPost("{id}/send-verification")]
    public async Task<IActionResult> SendVerificationCode(Guid id)
    {
        var result = await _reservationService.SendVerificationCodeAsync(id, GetOrganizationId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Verification code sent" });
    }

    /// <summary>
    /// Verify customer phone number
    /// </summary>
    [HttpPost("{id}/verify")]
    public async Task<IActionResult> VerifyPhone(Guid id, [FromBody] VerifyPhoneRequest request)
    {
        request.ReservationId = id;
        var result = await _reservationService.VerifyPhoneAsync(request, GetOrganizationId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Complete a reservation (mark items as sold)
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteReservation(Guid id, [FromBody] List<Guid>? purchasedItemIds = null)
    {
        var result = await _reservationService.CompleteReservationAsync(
            id, GetOrganizationId(), purchasedItemIds, GetUserId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Reservation completed" });
    }

    /// <summary>
    /// Cancel a reservation
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelReservation(Guid id, [FromBody] CancelReservationRequest? request = null)
    {
        var result = await _reservationService.CancelReservationAsync(
            id, GetOrganizationId(), request?.Reason, GetUserId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Reservation cancelled" });
    }

    /// <summary>
    /// Delete a reservation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservation(Guid id)
    {
        var result = await _reservationService.DeleteReservationAsync(id, GetOrganizationId(), GetUserId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = "Reservation deleted" });
    }

    /// <summary>
    /// Check if specific items are available for reservation
    /// </summary>
    [HttpPost("check-availability")]
    public async Task<IActionResult> CheckItemsAvailability([FromBody] List<Guid> itemIds)
    {
        var result = await _reservationService.CheckItemsAvailabilityAsync(itemIds, GetOrganizationId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, available = result.Data });
    }

    /// <summary>
    /// Get all reserved item IDs
    /// </summary>
    [HttpGet("reserved-items")]
    public async Task<IActionResult> GetReservedItems()
    {
        var result = await _reservationService.GetReservedItemIdsAsync(GetOrganizationId());

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Process expired reservations (background job endpoint)
    /// </summary>
    [HttpPost("process-expired")]
    public async Task<IActionResult> ProcessExpiredReservations()
    {
        var result = await _reservationService.ProcessExpiredReservationsAsync(GetOrganizationId());

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, processed = result.Data });
    }
}

/// <summary>
/// Public API controller for customer-facing reservation creation
/// </summary>
[ApiController]
[Route("api/public/reservations")]
public class PublicReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<PublicReservationsController> _logger;

    public PublicReservationsController(
        IReservationService reservationService,
        ILogger<PublicReservationsController> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a public reservation (customer-facing)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePublicReservation([FromBody] PublicCreateReservationRequest request)
    {
        var createRequest = new CreateReservationRequest
        {
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            ItemIds = request.ItemIds,
            CustomerNotes = request.CustomerNotes,
            HoldHours = 24 // Fixed 24 hour hold for public reservations
        };

        var result = await _reservationService.CreatePublicReservationAsync(createRequest, request.OrganizationId);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data });
    }

    /// <summary>
    /// Get public reservation status (customer-facing)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPublicReservation(Guid id, [FromQuery] string customerPhone)
    {
        var result = await _reservationService.GetPublicReservationAsync(id, customerPhone);

        if (!result.Success)
            return BadRequest(result.Message);

        return Ok(result.Data);
    }

    /// <summary>
    /// Verify phone for public reservation (customer-facing)
    /// </summary>
    [HttpPost("{id}/verify")]
    public async Task<IActionResult> VerifyPublicReservation(Guid id, [FromBody] PublicVerifyPhoneRequest request)
    {
        var verifyRequest = new VerifyPhoneRequest
        {
            ReservationId = id,
            VerificationCode = request.VerificationCode
        };

        var result = await _reservationService.VerifyPhoneAsync(verifyRequest, request.OrganizationId);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, data = result.Data });
    }
}

// Additional DTOs for public API
public class PublicCreateReservationRequest
{
    public Guid OrganizationId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public List<Guid> ItemIds { get; set; } = new();
    public string? CustomerNotes { get; set; }
}

public class PublicVerifyPhoneRequest
{
    public Guid OrganizationId { get; set; }
    public string VerificationCode { get; set; } = string.Empty;
}

public class CancelReservationRequest
{
    public string? Reason { get; set; }
}