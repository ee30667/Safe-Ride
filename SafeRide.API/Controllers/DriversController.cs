using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;

namespace SafeRide.API.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriversController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var drivers = await _driverService.GetAllAsync();
        return Ok(drivers.Select(ToDto));
    }

    [HttpGet("available")]
    [Authorize(Roles = "Admin,Passenger")]
    public async Task<IActionResult> GetAvailable()
    {
        var drivers = await _driverService.GetAvailableDriversAsync();
        return Ok(drivers.Select(ToDto));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> GetMyProfile()
    {
        var driver = await _driverService.GetByUserIdAsync(CurrentUserId);
        if (driver == null) return NotFound();

        return Ok(ToDto(driver));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var driver = await _driverService.GetByIdAsync(id);
        if (driver == null) return NotFound();

        return Ok(ToDto(driver));
    }

    [HttpPost]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Create([FromBody] CreateDriverProfileDto dto)
    {
        var existing = await _driverService.GetByUserIdAsync(CurrentUserId);

        if (existing != null)
            return BadRequest(new { message = "Driver profile already exists." });

        var profile = new DriverProfile
        {
            UserId = CurrentUserId,
            LicenseNumber = dto.LicenseNumber,
            Vehicle = new Vehicle
            {
                Make = dto.VehicleMake,
                Model = dto.VehicleModel,
                Color = dto.VehicleColor,
                LicensePlate = dto.LicensePlate,
                Year = dto.VehicleYear
            }
        };

        var created = await _driverService.CreateAsync(profile);
        var withUser = await _driverService.GetByIdAsync(created.Id);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(withUser!));
    }

    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id)
    {
        var driver = await _driverService.GetByIdAsync(id);
        if (driver == null) return NotFound();

        driver.IsApproved = true;
        driver.IsAvailable = true;
        driver.User.IsVerified = true;

        await _driverService.UpdateAsync(driver);

        return Ok(new { message = "Driver approved." });
    }

    [HttpPatch("availability")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> ToggleAvailability([FromBody] bool isAvailable)
    {
        var driver = await _driverService.GetByUserIdAsync(CurrentUserId);
        if (driver == null) return NotFound();

        if (!driver.IsApproved)
            return BadRequest(new { message = "Driver not yet approved." });

        driver.IsAvailable = isAvailable;

        await _driverService.UpdateAsync(driver);

        return Ok(new { isAvailable = driver.IsAvailable });
    }

    [HttpPatch("location")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDto dto)
    {
        var driver = await _driverService.GetByUserIdAsync(CurrentUserId);
        if (driver == null) return NotFound();

        driver.Latitude = dto.Latitude;
        driver.Longitude = dto.Longitude;

        await _driverService.UpdateAsync(driver);

        return Ok(new { message = "Location updated." });
    }

    private static DriverProfileDto ToDto(DriverProfile d) => new(
        d.Id,
        d.UserId,
        d.User?.FullName ?? "",
        d.LicenseNumber,
        d.IsAvailable,
        d.IsApproved,
        d.AverageRating,
        d.TotalRides,
        d.Vehicle == null
            ? null
            : new VehicleDto(
                d.Vehicle.Id,
                d.Vehicle.Make,
                d.Vehicle.Model,
                d.Vehicle.Color,
                d.Vehicle.LicensePlate,
                d.Vehicle.Year
            )
    );
}