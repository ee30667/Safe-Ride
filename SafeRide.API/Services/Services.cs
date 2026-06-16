using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;
using SafeRide.API.DTOs;

namespace SafeRide.API.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    public AuthService(IConfiguration config) => _config = config;

    public Task<string> GenerateTokenAsync(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }
}

public class RideService : IRideService
{
    private readonly IRideRepository _rideRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly AppDbContext _db;

    public RideService(IRideRepository rideRepo, IDriverRepository driverRepo, AppDbContext db)
    {
        _rideRepo = rideRepo;
        _driverRepo = driverRepo;
        _db = db;
    }

    public async Task<Ride> RequestRideAsync(int passengerId, RequestRideDto dto)
    {
        var existing = await _rideRepo.GetActiveRideForPassengerAsync(passengerId);
        if (existing != null) throw new InvalidOperationException("You already have an active ride.");

        var distance = CalculateDistance(dto.PickupLatitude, dto.PickupLongitude, dto.DropoffLatitude, dto.DropoffLongitude);
        var surge = await GetSurgeMultiplierAsync();
        var fare = await CalculateFareAsync(dto.PickupLatitude, dto.PickupLongitude, dto.DropoffLatitude, dto.DropoffLongitude);
        fare = Math.Round(fare * surge, 2);

        var ride = new Ride
        {
            PassengerId = passengerId,
            PickupAddress = dto.PickupAddress,
            PickupLatitude = dto.PickupLatitude,
            PickupLongitude = dto.PickupLongitude,
            DropoffAddress = dto.DropoffAddress,
            DropoffLatitude = dto.DropoffLatitude,
            DropoffLongitude = dto.DropoffLongitude,
            EstimatedFare = fare,
            DistanceKm = distance,
            SurgeMultiplier = surge,
            Status = RideStatus.Requested
        };
        return await _rideRepo.CreateAsync(ride);
    }

    public async Task<decimal> GetSurgeMultiplierAsync()
    {
        var activeRequests = await _db.Rides.CountAsync(r => r.Status == RideStatus.Requested);
        var availableDrivers = await _db.DriverProfiles.CountAsync(d => d.IsAvailable && d.IsApproved);
        if (availableDrivers == 0) return 1.0m;
        var ratio = (double)activeRequests / availableDrivers;
        if (ratio >= 3) return 2.0m;
        if (ratio >= 2) return 1.5m;
        if (ratio >= 1.5) return 1.2m;
        return 1.0m;
    }

    public async Task<List<DriverProfile>> GetNearbyDriversAsync(double lat, double lng, double radiusKm = 5)
    {
        var available = await _driverRepo.GetAvailableDriversAsync();
        return available.Where(d => CalculateDistance(d.Latitude, d.Longitude, lat, lng) <= radiusKm).ToList();
    }

    public async Task<Ride> AcceptRideAsync(int rideId, int driverProfileId)
    {
        var ride = await _rideRepo.GetByIdAsync(rideId) ?? throw new KeyNotFoundException("Ride not found.");
        if (ride.Status != RideStatus.Requested) throw new InvalidOperationException("Ride is not in Requested state.");
        var driver = await _driverRepo.GetByIdAsync(driverProfileId) ?? throw new KeyNotFoundException("Driver not found.");
        if (!driver.IsAvailable || !driver.IsApproved) throw new InvalidOperationException("Driver is not available.");

        ride.DriverProfileId = driverProfileId;
        ride.Status = RideStatus.Accepted;
        ride.AcceptedAt = DateTime.UtcNow;
        driver.IsAvailable = false;
        await _driverRepo.UpdateAsync(driver);

        _db.Notifications.Add(new Notification
        {
            UserId = ride.PassengerId,
            Title = "🚗 Driver Found!",
            Message = $"Your driver {driver.User.FullName} has accepted your ride. Look for {driver.Vehicle?.Make} {driver.Vehicle?.Model} — {driver.Vehicle?.LicensePlate}."
        });
        await _db.SaveChangesAsync();
        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<Ride> StartRideAsync(int rideId, int driverProfileId)
    {
        var ride = await _rideRepo.GetByIdAsync(rideId) ?? throw new KeyNotFoundException("Ride not found.");
        if (ride.Status != RideStatus.Accepted) throw new InvalidOperationException("Ride must be Accepted before starting.");
        if (ride.DriverProfileId != driverProfileId) throw new UnauthorizedAccessException("Not your ride.");
        ride.Status = RideStatus.InProgress;
        ride.StartedAt = DateTime.UtcNow;
        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<Ride> CompleteRideAsync(int rideId, int driverProfileId)
    {
        var ride = await _rideRepo.GetByIdAsync(rideId) ?? throw new KeyNotFoundException("Ride not found.");
        if (ride.Status != RideStatus.InProgress) throw new InvalidOperationException("Ride must be InProgress to complete.");
        if (ride.DriverProfileId != driverProfileId) throw new UnauthorizedAccessException("Not your ride.");

        ride.Status = RideStatus.Completed;
        ride.CompletedAt = DateTime.UtcNow;
        ride.FinalFare = ride.EstimatedFare;

        var driver = await _driverRepo.GetByIdAsync(driverProfileId);
        if (driver != null) { driver.IsAvailable = true; driver.TotalRides++; await _driverRepo.UpdateAsync(driver); }

        _db.Notifications.Add(new Notification
        {
            UserId = ride.PassengerId,
            Title = "✅ Ride Completed",
            Message = $"Your ride is complete! Total: {ride.FinalFare} MKD. Please rate your driver."
        });
        await _db.SaveChangesAsync();
        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<Ride> CancelRideAsync(int rideId, int userId, string reason = "")
    {
        var ride = await _rideRepo.GetByIdAsync(rideId) ?? throw new KeyNotFoundException("Ride not found.");
        if (ride.Status == RideStatus.Completed || ride.Status == RideStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a completed or already cancelled ride.");

        ride.Status = RideStatus.Cancelled;
        ride.CancellationReason = reason;

        if (ride.DriverProfileId.HasValue)
        {
            var driver = await _driverRepo.GetByIdAsync(ride.DriverProfileId.Value);
            if (driver != null) { driver.IsAvailable = true; await _driverRepo.UpdateAsync(driver); }
        }
        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<decimal> CalculateFareAsync(double pickupLat, double pickupLng, double dropoffLat, double dropoffLng)
    {
        var distanceKm = CalculateDistance(pickupLat, pickupLng, dropoffLat, dropoffLng);
        decimal baseFare = 50m;       // 50 MKD base
        decimal perKm = 30m;          // 30 MKD per km
        return baseFare + (decimal)distanceKm * perKm;
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
    private static double ToRad(double deg) => deg * Math.PI / 180;
}
