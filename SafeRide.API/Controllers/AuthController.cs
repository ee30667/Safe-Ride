using Microsoft.AspNetCore.Mvc;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;

namespace SafeRide.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public AuthController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _userService.GetByEmailAsync(dto.Email) != null)
            return BadRequest(new { message = "Email already registered." });

        var allowedRoles = new[] { "Passenger", "Driver" };

        if (!allowedRoles.Contains(dto.Role))
            return BadRequest(new { message = "Role must be Passenger or Driver." });

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber,
            Role = dto.Role,
            IsVerified = dto.Role == "Passenger"
        };

        var created = await _userService.CreateAsync(user);
        var token = await _authService.GenerateTokenAsync(created);

        return Ok(new AuthResponseDto(token, created.Role, created.FullName, created.Id));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userService.GetByEmailAsync(dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        if (!user.IsActive)
            return Unauthorized(new { message = "Account is deactivated." });

        var token = await _authService.GenerateTokenAsync(user);

        return Ok(new AuthResponseDto(token, user.Role, user.FullName, user.Id));
    }
}