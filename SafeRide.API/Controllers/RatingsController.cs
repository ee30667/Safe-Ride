using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;

namespace SafeRide.API.Controllers;

[ApiController]
[Route("api/ratings")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> Create([FromBody] CreateRatingDto dto)
    {
        try
        {
            var rating = await _ratingService.CreateRatingAsync(CurrentUserId, dto);
            return Ok(rating);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("driver/{driverProfileId}")]
    public async Task<IActionResult> GetDriverRatings(int driverProfileId)
    {
        var ratings = await _ratingService.GetDriverRatingsAsync(driverProfileId);
        return Ok(ratings);
    }
}