using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DevController : ControllerBase
{
    private readonly SeedDataService _seedDataService;

    public DevController(SeedDataService seedDataService)
    {
        _seedDataService = seedDataService;
    }

    [HttpPost("seed-demo-data")]
    public async Task<ActionResult<ApiResponse<string>>> SeedDemoData()
    {
        try
        {
            await _seedDataService.SeedDemoDataAsync();
            return Ok(ApiResponse<string>.SuccessResult("Demo data seeded successfully", "Seed operation completed"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResult($"Seed failed: {ex.Message}"));
        }
    }
}