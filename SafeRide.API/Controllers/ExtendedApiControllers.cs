using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;
using SafeRide.API.Services;
using System.Security.Claims;

namespace SafeRide.API.Controllers;

// ── Driver Location ────────────────────────────────────────────────────────────
[ApiController]
[Route("api/driver-location")]
[Authorize]
public class DriverLocationApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public DriverLocationApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("update")]
    [Authorize(Roles = "Driver")]
    public async Task<IActionResult> Update([FromBody] LocationDto dto)
    {
        var driver = await _db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == UserId);
        if (driver == null) return NotFound();

        driver.Latitude = dto.Latitude;
        driver.Longitude = dto.Longitude;

        var loc = await _db.DriverLocations.FirstOrDefaultAsync(l => l.DriverProfileId == driver.Id);
        if (loc == null)
            _db.DriverLocations.Add(new DriverLocation { DriverProfileId = driver.Id, Latitude = dto.Latitude, Longitude = dto.Longitude });
        else
        { loc.Latitude = dto.Latitude; loc.Longitude = dto.Longitude; loc.UpdatedAt = DateTime.UtcNow; }

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("{driverProfileId}")]
    public async Task<IActionResult> Get(int driverProfileId)
    {
        var loc = await _db.DriverLocations.FirstOrDefaultAsync(l => l.DriverProfileId == driverProfileId);
        if (loc == null) return NotFound(new { found = false });
        return Ok(new { found = true, latitude = loc.Latitude, longitude = loc.Longitude, updatedAt = loc.UpdatedAt });
    }
}
public record LocationDto(double Latitude, double Longitude);

// ── Favorites ──────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public FavoritesApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var favs = await _db.FavoriteLocations
            .Where(f => f.UserId == UserId)
            .OrderBy(f => f.Label)
            .Select(f => new { f.Id, f.Label, f.Address, f.Latitude, f.Longitude })
            .ToListAsync();
        return Ok(favs);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] FavoriteDto dto)
    {
        _db.FavoriteLocations.Add(new FavoriteLocation { UserId = UserId, Label = dto.Label, Address = dto.Address, Latitude = dto.Latitude, Longitude = dto.Longitude });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Favorite saved." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var fav = await _db.FavoriteLocations.FindAsync(id);
        if (fav == null || fav.UserId != UserId) return NotFound();
        _db.FavoriteLocations.Remove(fav);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted." });
    }
}
public record FavoriteDto(string Label, string Address, double Latitude, double Longitude);

// ── Chat ───────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChatApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{rideId}")]
    public async Task<IActionResult> GetMessages(int rideId, [FromQuery] int lastId = 0)
    {
        var messages = await _db.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.RideId == rideId && m.Id > lastId)
            .OrderBy(m => m.SentAt)
            .Select(m => new { m.Id, m.Message, m.SentAt, SenderName = m.Sender.FullName, m.SenderId })
            .ToListAsync();
        return Ok(messages);
    }

    [HttpPost("{rideId}")]
    public async Task<IActionResult> Send(int rideId, [FromBody] SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest();
        _db.ChatMessages.Add(new ChatMessage { RideId = rideId, SenderId = UserId, Message = dto.Message.Trim() });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Sent." });
    }
}
public record SendMessageDto(string Message);

// ── Notifications ──────────────────────────────────────────────────────────────
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notifs = await _db.Notifications
            .Where(n => n.UserId == UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Id, n.Title, n.Message, n.IsRead, n.CreatedAt })
            .ToListAsync();
        return Ok(notifs);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _db.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);
        return Ok(new { count });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null || n.UserId != UserId) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}

// ── SOS ────────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/sos")]
[Authorize]
public class SosApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public SosApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("trigger")]
    public async Task<IActionResult> Trigger([FromBody] SosTriggerDto dto)
    {
        var alert = new SosAlert { UserId = UserId, RideId = dto.RideId, Latitude = dto.Latitude, Longitude = dto.Longitude };
        _db.SosAlerts.Add(alert);

        var admins = await _db.Users.Where(u => u.Role == "Admin").ToListAsync();
        foreach (var admin in admins)
            _db.Notifications.Add(new Notification { UserId = admin.Id, Title = "🚨 SOS ALERT", Message = $"User {UserId} triggered SOS at {dto.Latitude:F4}, {dto.Longitude:F4}." });

        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpGet("alerts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Alerts()
    {
        var alerts = await _db.SosAlerts
            .Include(s => s.User)
            .Include(s => s.Ride)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.Id, s.UserId, UserName = s.User.FullName, s.RideId, s.Latitude, s.Longitude, s.IsResolved, s.CreatedAt })
            .ToListAsync();
        return Ok(alerts);
    }

    [HttpPatch("alerts/{id}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Resolve(int id)
    {
        var alert = await _db.SosAlerts.FindAsync(id);
        if (alert == null) return NotFound();
        alert.IsResolved = true;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
public record SosTriggerDto(int? RideId, double Latitude, double Longitude);

// ── Receipt ────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/receipt")]
[Authorize]
public class ReceiptApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReceiptApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet("{rideId}")]
    public async Task<IActionResult> Get(int rideId)
    {
        var ride = await _db.Rides
            .Include(r => r.Passenger)
            .Include(r => r.DriverProfile).ThenInclude(d => d!.User)
            .Include(r => r.DriverProfile).ThenInclude(d => d!.Vehicle)
            .Include(r => r.Rating)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == rideId);

        if (ride == null) return NotFound();
        if (ride.PassengerId != UserId && ride.DriverProfile?.UserId != UserId && Role != "Admin")
            return Forbid();

        var duration = ride.StartedAt.HasValue && ride.CompletedAt.HasValue
            ? (int)(ride.CompletedAt.Value - ride.StartedAt.Value).TotalMinutes : 0;

        return Ok(new
        {
            rideId = ride.Id,
            passengerName = ride.Passenger.FullName,
            driverName = ride.DriverProfile?.User.FullName ?? "N/A",
            vehicleInfo = ride.DriverProfile?.Vehicle != null
                ? $"{ride.DriverProfile.Vehicle.Year} {ride.DriverProfile.Vehicle.Make} {ride.DriverProfile.Vehicle.Model} — {ride.DriverProfile.Vehicle.LicensePlate}"
                : "N/A",
            pickupAddress = ride.PickupAddress,
            dropoffAddress = ride.DropoffAddress,
            distanceKm = ride.DistanceKm ?? 0,
            baseFare = 50m,
            distanceFare = (decimal)(ride.DistanceKm ?? 0) * 30m,
            surgeMultiplier = ride.SurgeMultiplier,
            totalFare = ride.FinalFare ?? ride.EstimatedFare,
            paymentMethod = ride.Payment?.Method ?? ride.PaymentMethod,
            completedAt = ride.CompletedAt ?? ride.RequestedAt,
            durationMinutes = duration
        });
    }
}

// ── Analytics ──────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnalyticsApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var now = DateTime.UtcNow;
        var rides = await _db.Rides.Include(r => r.Payment).ToListAsync();

        var ridesPerDay = Enumerable.Range(0, 7).Select(i =>
        {
            var day = now.Date.AddDays(-6 + i);
            return new { date = day.ToString("dd MMM"), count = rides.Count(r => r.RequestedAt.Date == day) };
        }).ToList();

        var revenuePerDay = Enumerable.Range(0, 7).Select(i =>
        {
            var day = now.Date.AddDays(-6 + i);
            return new { date = day.ToString("dd MMM"), amount = rides.Where(r => r.CompletedAt?.Date == day).Sum(r => r.FinalFare ?? 0) };
        }).ToList();

        var peakHours = Enumerable.Range(0, 24).Select(h =>
            new { hour = $"{h}:00", count = rides.Count(r => r.RequestedAt.Hour == h) }).ToList();

        var statusBreakdown = Enum.GetValues<RideStatus>().Select(s =>
            new { status = s.ToString(), count = rides.Count(r => r.Status == s) }).ToList();

        return Ok(new
        {
            ridesPerDay,
            revenuePerDay,
            peakHours,
            statusBreakdown,
            totalRevenue = rides.Where(r => r.Status == RideStatus.Completed).Sum(r => r.FinalFare ?? 0),
            totalRides = rides.Count,
            completionRate = rides.Any() ? Math.Round((double)rides.Count(r => r.Status == RideStatus.Completed) / rides.Count * 100, 1) : 0,
            activeDrivers = await _db.DriverProfiles.CountAsync(d => d.IsAvailable && d.IsApproved)
        });
    }
}

// ── Earnings ───────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/earnings")]
[Authorize(Roles = "Driver")]
public class EarningsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public EarningsApiController(AppDbContext db) => _db = db;
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var driver = await _db.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == UserId);
        if (driver == null) return NotFound();

        var completed = await _db.Rides
            .Include(r => r.Payment)
            .Where(r => r.DriverProfileId == driver.Id && r.Status == RideStatus.Completed)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var thisWeek = today.AddDays(-(int)today.DayOfWeek);
        var thisMonth = new DateTime(today.Year, today.Month, 1);

        return Ok(new
        {
            totalEarnings = completed.Sum(r => r.FinalFare ?? 0),
            todayEarnings = completed.Where(r => r.CompletedAt?.Date == today).Sum(r => r.FinalFare ?? 0),
            weekEarnings = completed.Where(r => r.CompletedAt >= thisWeek).Sum(r => r.FinalFare ?? 0),
            monthEarnings = completed.Where(r => r.CompletedAt >= thisMonth).Sum(r => r.FinalFare ?? 0),
            rides = completed.Select(r => new { r.Id, r.PickupAddress, r.DropoffAddress, r.FinalFare, r.CompletedAt })
        });
    }
}

// ── Payment ────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/payment")]
[Authorize(Roles = "Passenger")]
public class PaymentApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    public PaymentApiController(AppDbContext db, IConfiguration config) { _db = db; _config = config; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{rideId}/create-intent")]
    public async Task<IActionResult> CreateIntent(int rideId)
    {
        var ride = await _db.Rides.FirstOrDefaultAsync(r => r.Id == rideId && r.PassengerId == UserId);
        if (ride == null) return NotFound();

        Stripe.StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        var options = new Stripe.PaymentIntentCreateOptions
        {
            Amount = (long)(ride.EstimatedFare * 100),
            Currency = "mkd",
            AutomaticPaymentMethods = new Stripe.PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };
        var service = new Stripe.PaymentIntentService();
        var intent = await service.CreateAsync(options);

        _db.Payments.Add(new Payment { RideId = rideId, Method = "Card", Amount = ride.EstimatedFare, StripePaymentIntentId = intent.Id });
        ride.PaymentMethod = "Card";
        await _db.SaveChangesAsync();

        return Ok(new { clientSecret = intent.ClientSecret });
    }

    [HttpPost("{rideId}/cash")]
    public async Task<IActionResult> CashPayment(int rideId)
    {
        var ride = await _db.Rides
            .Include(r => r.DriverProfile).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(r => r.Id == rideId && r.PassengerId == UserId);
        if (ride == null) return NotFound();

        _db.Payments.Add(new Payment { RideId = rideId, Method = "Cash", Amount = ride.FinalFare ?? ride.EstimatedFare, Status = "Completed" });

        if (ride.DriverProfile != null)
            _db.Notifications.Add(new Notification { UserId = ride.DriverProfile.UserId, Title = "💵 Cash Payment Received", Message = $"Passenger paid {(ride.FinalFare ?? ride.EstimatedFare):F2} MKD cash for ride #{rideId}." });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Cash payment recorded." });
    }
}

// ── Admin ──────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminApiController(AppDbContext db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var users = await _db.Users.CountAsync();
        var drivers = await _db.DriverProfiles.CountAsync();
        var rides = await _db.Rides.CountAsync();
        var completed = await _db.Rides.CountAsync(r => r.Status == RideStatus.Completed);
        var pending = await _db.DriverProfiles
            .Where(d => !d.IsApproved)
            .Select(d => new { d.Id, d.UserId, d.LicenseNumber, d.IsApproved })
            .ToListAsync();
        return Ok(new { totalUsers = users, totalDrivers = drivers, totalRides = rides, completedRides = completed, pendingDrivers = pending });
    }

    [HttpPost("drivers/{driverId}/approve")]
    public async Task<IActionResult> ApproveDriver(int driverId)
    {
        var driver = await _db.DriverProfiles.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == driverId);
        if (driver == null) return NotFound();
        driver.IsApproved = true;
        driver.IsAvailable = true; // auto-set online when approved
        driver.User.IsVerified = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Driver approved." });
    }

    [HttpPost("users/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deactivated." });
    }
}

// ── Passenger: find drivers + book ────────────────────────────────────────────
[ApiController]
[Route("api/passenger")]
[Authorize(Roles = "Passenger")]
public class PassengerApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IRideService _rideService;
    public PassengerApiController(AppDbContext db, IRideService rideService) { _db = db; _rideService = rideService; }
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Tetovo bounding box
    private static bool IsInTetovo(double lat, double lng) =>
        lat >= 41.95 && lat <= 42.05 &&
        lng >= 20.90 && lng <= 21.10;

    [HttpGet("find-drivers")]
    public async Task<IActionResult> FindDrivers(
        [FromQuery] double pickupLat, [FromQuery] double pickupLng,
        [FromQuery] double dropoffLat, [FromQuery] double dropoffLng)
    {
        if (!IsInTetovo(pickupLat, pickupLng) || !IsInTetovo(dropoffLat, dropoffLng))
            return BadRequest(new { message = "SafeRide only operates within Tetovo." });

        var available = await _db.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .Where(d => d.IsAvailable && d.IsApproved && d.User.IsActive)
            .ToListAsync();

        var surge = await _rideService.GetSurgeMultiplierAsync();
        var fare = await _rideService.CalculateFareAsync(pickupLat, pickupLng, dropoffLat, dropoffLng);
        fare = Math.Round(fare * surge, 2);
        var distance = RideService.CalculateDistance(pickupLat, pickupLng, dropoffLat, dropoffLng);

        var options = available.Select(d => new
        {
            d.Id,
            d.UserId,
            FullName = d.User.FullName,
            d.AverageRating,
            d.TotalRides,
            VehicleMake = d.Vehicle?.Make ?? "",
            VehicleModel = d.Vehicle?.Model ?? "",
            VehicleColor = d.Vehicle?.Color ?? "",
            VehicleYear = d.Vehicle?.Year ?? 0,
            LicensePlate = d.Vehicle?.LicensePlate ?? "",
            DistanceToPickup = Math.Round(RideService.CalculateDistance(d.Latitude, d.Longitude, pickupLat, pickupLng), 1),
            EtaMinutes = Math.Max(1, (int)Math.Round(RideService.CalculateDistance(d.Latitude, d.Longitude, pickupLat, pickupLng) / 0.5))
        }).OrderBy(d => d.DistanceToPickup).ToList();

        return Ok(new { drivers = options, estimatedFare = fare, distanceKm = Math.Round(distance, 1), surgeMultiplier = surge });
    }

    [HttpPost("book")]
    public async Task<IActionResult> Book([FromBody] BookRideDto dto)
    {
        if (!IsInTetovo(dto.PickupLat, dto.PickupLng) || !IsInTetovo(dto.DropoffLat, dto.DropoffLng))
            return BadRequest(new { message = "SafeRide only operates within Tetovo." });

        var existing = await _db.Rides.FirstOrDefaultAsync(r =>
            r.PassengerId == UserId &&
            (r.Status == RideStatus.Requested || r.Status == RideStatus.Accepted || r.Status == RideStatus.InProgress));
        if (existing != null) return BadRequest(new { message = "You already have an active ride." });

        var surge = await _rideService.GetSurgeMultiplierAsync();
        var fare = await _rideService.CalculateFareAsync(dto.PickupLat, dto.PickupLng, dto.DropoffLat, dto.DropoffLng);
        fare = Math.Round(fare * surge, 2);
        var distance = RideService.CalculateDistance(dto.PickupLat, dto.PickupLng, dto.DropoffLat, dto.DropoffLng);

        var ride = new Ride
        {
            PassengerId = UserId,
            DriverProfileId = dto.DriverProfileId,
            PickupAddress = dto.PickupAddress,
            PickupLatitude = dto.PickupLat,
            PickupLongitude = dto.PickupLng,
            DropoffAddress = dto.DropoffAddress,
            DropoffLatitude = dto.DropoffLat,
            DropoffLongitude = dto.DropoffLng,
            EstimatedFare = fare,
            DistanceKm = distance,
            SurgeMultiplier = surge,
            PaymentMethod = dto.PaymentMethod ?? "Cash",
            Status = RideStatus.Requested
        };

        if (!string.IsNullOrEmpty(dto.ScheduledFor) &&
            DateTime.TryParse(dto.ScheduledFor, out var scheduled) &&
            scheduled > DateTime.UtcNow)
        {
            ride.IsScheduled = true;
            ride.ScheduledFor = scheduled.ToUniversalTime();
        }

        _db.Rides.Add(ride);

        // Notify driver but do NOT set IsAvailable = false yet
        // Driver availability is set to false only when they ACCEPT the ride
        var driver = await _db.DriverProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dto.DriverProfileId);
        if (driver != null)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = driver.UserId,
                Title = "🚗 New Ride Request",
                Message = $"A passenger chose you! {dto.PickupAddress} → {dto.DropoffAddress}. Fare: {fare} MKD."
            });
            // Do NOT set driver.IsAvailable = false here
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Ride booked!", rideId = ride.Id });
    }
}

public record BookRideDto(
    int DriverProfileId,
    string PickupAddress, double PickupLat, double PickupLng,
    string DropoffAddress, double DropoffLat, double DropoffLng,
    string? PaymentMethod, string? ScheduledFor);