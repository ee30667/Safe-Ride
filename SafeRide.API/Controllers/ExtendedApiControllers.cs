using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;
using SafeRide.API.Services;
using Stripe;
using System.Security.Claims;

namespace SafeRide.API.Controllers;

//  Driver Location
[ApiController]
[Route("api/driver-location")]
[Authorize]
public class DriverLocationApiController : ControllerBase
{
    private readonly IDriverLocationService _service;

    public DriverLocationApiController(IDriverLocationService service)
    {
        _service = service;
    }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("update")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Update([FromBody] LocationDto dto)
    {
        var result = await _service.UpdateLocationAsync(
        UserId,
        dto.Latitude,
        dto.Longitude);

        if (!result)
            return NotFound();

        return Ok(new { success = true });
    }

    [HttpGet("{driverProfileId}")]
    public async Task<IActionResult> Get(int driverProfileId)
    {

        var loc = await _service.GetLocationAsync(driverProfileId);

        if (loc == null)
            return NotFound(new { found = false });

        return Ok(new
        {
            found = true,
            latitude = loc.Latitude,
            longitude = loc.Longitude,
            updatedAt = loc.UpdatedAt
        });
    }
}
public record LocationDto(double Latitude, double Longitude);

//  Favorites
[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesApiController : ControllerBase
{
    private readonly IFavoriteService _service;
    public FavoritesApiController(IFavoriteService service)
    {
        _service = service;
    }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var favs = await _service.GetAllAsync(UserId);
        return Ok(favs);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] FavoriteDto dto)
    {
        await _service.AddAsync(UserId, dto);
        return Ok(new { message = "Favorite saved." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(UserId, id);

        if (!result)
            return NotFound();

        return Ok(new { message = "Deleted." });
    }
}
public record FavoriteDto(string Label, string Address, double Latitude, double Longitude);

// Chat
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatApiController : ControllerBase
{
    private readonly IChatService _service;
    public ChatApiController(IChatService service)
    { _service = service; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{rideId}")]
    public async Task<IActionResult> GetMessages(int rideId, [FromQuery] int lastId = 0)
    {
        var messages = await _service.GetMessagesAsync(
        rideId,
        lastId);

        return Ok(messages);
    }

    [HttpPost("{rideId}")]
    public async Task<IActionResult> Send(int rideId, [FromBody] SendMessageDto dto)
    {
        await _service.SendAsync(
        rideId,
        UserId,
        dto.Message);

        return Ok(new { message = "Sent." });
    }
}
public record SendMessageDto(string Message);

//  Notifications 
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsApiController : ControllerBase
{
    private readonly INotificationService _service;
    public NotificationsApiController(INotificationService service)
    {
        _service = service;
    }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync(UserId));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        return Ok(new
        {
            count = await _service.UnreadCountAsync(UserId)
        });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var success = await _service.MarkReadAsync(UserId, id);

        if (!success)
            return NotFound();

        return Ok();
    }
}

//  SOS 
[ApiController]
[Route("api/sos")]
[Authorize]
public class SosApiController : ControllerBase
{
    private readonly ISosService _service;
    public SosApiController(ISosService service)
    {
        _service = service;
    }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger([FromBody] SosTriggerDto dto)
    {
        await _service.TriggerAsync(UserId, dto);

        return Ok(new
        {
            success = true
        });
    }

    [HttpGet("alerts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Alerts()
    {
        return Ok(await _service.GetAlertsAsync());
    }

    [HttpPatch("alerts/{id}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Resolve(int id)
    {
        var success = await _service.ResolveAsync(id);

        if (!success)
            return NotFound();

        return Ok();
    }
}
public record SosTriggerDto(int? RideId, double Latitude, double Longitude);

// ── Receipt 
[ApiController]
[Route("api/receipt")]
[Authorize]
public class ReceiptApiController : ControllerBase
{
    private readonly IReceiptService _service;
    public ReceiptApiController(IReceiptService service)
    {
        _service = service;
    }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet("{rideId}")]
    public async Task<IActionResult> Get(int rideId)
    {
        try
        {
            var receipt =
                await _service.GetReceiptAsync(
                    rideId,
                    UserId,
                    Role);

            if (receipt == null)
                return NotFound();

            return Ok(receipt);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

// ── Analytics ──────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsApiController : ControllerBase
{
    private readonly IAnalyticsService _service;

    public AnalyticsApiController(IAnalyticsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _service.GetAnalyticsAsync());
    }
}

// ── Earnings ───────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/earnings")]
[Authorize(Roles = "Driver")]
public class EarningsApiController : ControllerBase
{
    private readonly IEarningsService _service;

    public EarningsApiController(IEarningsService service)
    {
        _service = service;
    }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var earnings = await _service.GetEarningsAsync(UserId);

        if (earnings == null)
            return NotFound();

        return Ok(earnings);
    }
}

// ── Payment ────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/payment")]
[Authorize(Roles = "Passenger")]
public class PaymentApiController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly IConfiguration _config;
    public PaymentApiController(IPaymentService service, IConfiguration config) { _service=service; _config = config; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{rideId}/create-intent")]
    public async Task<IActionResult> CreateIntent(int rideId)
    {
        var result =
        await _service.CreateCardIntentAsync(
         rideId,
         UserId);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{rideId}/cash")]
    public async Task<IActionResult> CashPayment(int rideId)
    {
        var success =
     await _service.CashPaymentAsync(
         rideId,
         UserId);

        if (!success)
            return NotFound();

        return Ok(new
        {
            message = "Cash payment recorded."
        });
    }
}

// ── Admin ──────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminApiController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminApiController(IAdminService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        return Ok(await _service.DashboardAsync());
    }

    [HttpPost("drivers/{driverId}/approve")]
    public async Task<IActionResult> ApproveDriver(int driverId)
    {
        var success =
         await _service.ApproveDriverAsync(driverId);

        if (!success)
            return NotFound();

        return Ok(new
        {
            message = "Driver approved."
        });
    }

    [HttpPost("users/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int userId)
    {
        var success =
        await _service.DeactivateUserAsync(userId);

        if (!success)
            return NotFound();

        return Ok(new
        {
            message = "User deactivated."
        });
    }
}

// ── Passenger: find drivers + book ────────────────────────────────────────────
// ── Passenger: find drivers + book ────────────────────────────────────────────
[ApiController]
[Route("api/passenger")]
[Authorize(Roles = "Passenger")]
public class PassengerApiController : ControllerBase
{
    private readonly IRideService _rideService;
    public PassengerApiController(IRideService rideService) { _rideService = rideService; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("find-drivers")]
    public async Task<IActionResult> FindDrivers(
        [FromQuery] double pickupLat, [FromQuery] double pickupLng,
        [FromQuery] double dropoffLat, [FromQuery] double dropoffLng)
    {
        try
        {
            var result = await _rideService.FindDriversAsync(pickupLat, pickupLng, dropoffLat, dropoffLng);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("book")]
    public async Task<IActionResult> Book([FromBody] BookRideDto dto)
    {
        try
        {
            var rideId = await _rideService.BookRideAsync(UserId, dto);
            return Ok(new { message = "Ride booked!", rideId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record BookRideDto(
    int DriverProfileId,
    string PickupAddress, double PickupLat, double PickupLng,
    string DropoffAddress, double DropoffLat, double DropoffLng,
    string? PaymentMethod, string? ScheduledFor);