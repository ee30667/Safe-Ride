using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;

namespace SafeRide.API.Controllers;

[ApiController]
[Route("api/ratings")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingRepository _ratingRepo;
    private readonly IRideRepository _rideRepo;
    private readonly IDriverRepository _driverRepo;

    public RatingsController(IRatingRepository ratingRepo, IRideRepository rideRepo, IDriverRepository driverRepo)
    {
        _ratingRepo = ratingRepo;
        _rideRepo = rideRepo;
        _driverRepo = driverRepo;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> Create([FromBody] CreateRatingDto dto)
    {
        if (dto.Score < 1 || dto.Score > 5)
            return BadRequest(new { message = "Score must be between 1 and 5." });

        var ride = await _rideRepo.GetByIdAsync(dto.RideId);
        if (ride == null) return NotFound(new { message = "Ride not found." });
        if (ride.PassengerId != CurrentUserId) return Forbid();
        if (ride.Status != RideStatus.Completed) return BadRequest(new { message = "Can only rate completed rides." });

        var existing = await _ratingRepo.GetByRideIdAsync(dto.RideId);
        if (existing != null) return BadRequest(new { message = "Ride already rated." });

        var rating = new RideRating { RideId = dto.RideId, RaterId = CurrentUserId, Score = dto.Score, Comment = dto.Comment };
        var created = await _ratingRepo.CreateAsync(rating);

        if (ride.DriverProfileId.HasValue)
        {
            var driver = await _driverRepo.GetByIdAsync(ride.DriverProfileId.Value);
            if (driver != null)
            {
                var allRatings = await _ratingRepo.GetByDriverAsync(driver.Id);
                driver.AverageRating = Math.Round(allRatings.Average(r => r.Score), 2);
                driver.Rating1Count = allRatings.Count(r => r.Score == 1);
                driver.Rating2Count = allRatings.Count(r => r.Score == 2);
                driver.Rating3Count = allRatings.Count(r => r.Score == 3);
                driver.Rating4Count = allRatings.Count(r => r.Score == 4);
                driver.Rating5Count = allRatings.Count(r => r.Score == 5);
                await _driverRepo.UpdateAsync(driver);
            }
        }
        return Ok(new RatingDto(created.Id, created.RideId, "", created.Score, created.Comment, created.CreatedAt));
    }

    [HttpGet("driver/{driverProfileId}")]
    public async Task<IActionResult> GetDriverRatings(int driverProfileId)
    {
        var ratings = await _ratingRepo.GetByDriverAsync(driverProfileId);
        return Ok(ratings.Select(r => new RatingDto(r.Id, r.RideId, r.Rater.FullName, r.Score, r.Comment, r.CreatedAt)));
    }
}
