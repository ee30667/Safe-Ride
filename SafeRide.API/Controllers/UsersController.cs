using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;

namespace SafeRide.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();

        return Ok(users.Select(u =>
            new UserDto(u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role,
                u.IsVerified, u.IsActive, u.CreatedAt)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (CurrentRole != "Admin" && CurrentUserId != id)
            return Forbid();

        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.PhoneNumber,
            user.Role, user.IsVerified, user.IsActive, user.CreatedAt));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        if (CurrentRole != "Admin" && CurrentUserId != id)
            return Forbid();

        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;

        var updated = await _userService.UpdateAsync(user);

        return Ok(new UserDto(updated.Id, updated.FullName, updated.Email,
            updated.PhoneNumber, updated.Role, updated.IsVerified,
            updated.IsActive, updated.CreatedAt));
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = false;
        await _userService.UpdateAsync(user);

        return Ok(new { message = "User deactivated." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _userService.ExistsAsync(id))
            return NotFound();

        await _userService.DeleteAsync(id);

        return NoContent();
    }
}