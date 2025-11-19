using ConsignmentGenie.Application.DTOs;
using ConsignmentGenie.Application.DTOs.Auth;
using ConsignmentGenie.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsignmentGenie.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid email or password"));
            }

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult($"Login failed: {ex.Message}"));
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);

            if (response == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Email already exists"));
            }

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Registration successful"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResult($"Registration failed: {ex.Message}"));
        }
    }
}