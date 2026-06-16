using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;

namespace SafeRide.API.Controllers;

[ApiController]
[Route("api/rides")]
[Authorize]
public class RidesController : ControllerBase
{
    private readonly IRideRepository _rideRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IRideService _rideService;

    public RidesController(IRideRepository rideRepo, IDriverRepository driverRepo, IRideService rideService)
    {
        _rideRepo = rideRepo;
        _driverRepo = driverRepo;
        _rideService = rideService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var rides = await _rideRepo.GetAllAsync();
        return Ok(rides.Select(ToDto));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRides()
    {
        if (CurrentRole == "Passenger")
        {
            var rides = await _rideRepo.GetByPassengerIdAsync(CurrentUserId);
            return Ok(rides.Select(ToDto));
        }
        if (CurrentRole == "Driver")
        {
            var driver = await _driverRepo.GetByUserIdAsync(CurrentUserId);
            if (driver == null) return NotFound(new { message = "Driver profile not found." });
            var rides = await _rideRepo.GetByDriverProfileIdAsync(driver.Id);
            return Ok(rides.Select(ToDto));
        }
        return Forbid();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ride = await _rideRepo.GetByIdAsync(id);
        if (ride == null) return NotFound();
        if (CurrentRole != "Admin" && ride.PassengerId != CurrentUserId)
        {
            var driver = await _driverRepo.GetByUserIdAsync(CurrentUserId);
            if (driver == null || ride.DriverProfileId != driver.Id) return Forbid();
        }
        return Ok(ToDto(ride));
    }

    [HttpPost]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> RequestRide([FromBody] RequestRideDto dto)
    {
        try
        {
            var ride = await _rideService.RequestRideAsync(CurrentUserId, dto);
            return CreatedAtAction(nameof(GetById), new { id = ride.Id }, ToDto(ride));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/accept")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Accept(int id)
    {
        var driver = await _driverRepo.GetByUserIdAsync(CurrentUserId);
        if (driver == null) return NotFound(new { message = "Driver profile not found." });
        if (!driver.IsApproved) return BadRequest(new { message = "Your profile is not approved yet." });
        if (!driver.IsAvailable) return BadRequest(new { message = "You are offline. Go online first to accept rides." });

        try
        {
            var ride = await _rideService.AcceptRideAsync(id, driver.Id);
            // Set driver offline now that they have accepted
            driver.IsAvailable = false;
            await _driverRepo.UpdateAsync(driver);
            return Ok(ToDto(ride));
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
    [HttpPatch("{id}/start")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Start(int id)
    {
        var driver = await _driverRepo.GetByUserIdAsync(CurrentUserId);
        if (driver == null) return NotFound(new { message = "Driver profile not found." });
        try { return Ok(ToDto(await _rideService.StartRideAsync(id, driver.Id))); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/complete")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Complete(int id)
    {
        var driver = await _driverRepo.GetByUserIdAsync(CurrentUserId);
        if (driver == null) return NotFound(new { message = "Driver profile not found." });
        try { return Ok(ToDto(await _rideService.CompleteRideAsync(id, driver.Id))); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        try { return Ok(ToDto(await _rideService.CancelRideAsync(id, CurrentUserId))); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private static RideDto ToDto(Models.Ride r) => new(
        r.Id, r.PassengerId, r.Passenger?.FullName ?? "",
        r.DriverProfileId, r.DriverProfile?.User?.FullName,
        r.PickupAddress, r.DropoffAddress,
        r.Status.ToString(), r.EstimatedFare, r.FinalFare,
        r.DistanceKm, r.RequestedAt, r.CompletedAt);
}
