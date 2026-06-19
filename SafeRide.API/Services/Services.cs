using Microsoft.IdentityModel.Tokens;
using SafeRide.API.Controllers;
using SafeRide.API.DTOs;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SafeRide.API.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;

    public AuthService(IConfiguration config)
    {
        _config = config;
    }

    public Task<string> GenerateTokenAsync(User user)
    {
        var jwtKey = _config["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            jwtKey = "SafeRideSuperSecretJwtKey2026VeryLongAndSecure123";
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
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
            signingCredentials: creds
        );

        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }
}

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public Task<User?> GetByIdAsync(int id) => _users.GetByIdAsync(id);
    public Task<User?> GetByEmailAsync(string email) => _users.GetByEmailAsync(email);
    public Task<IEnumerable<User>> GetAllAsync() => _users.GetAllAsync();
    public Task<User> CreateAsync(User user) => _users.CreateAsync(user);
    public Task<User> UpdateAsync(User user) => _users.UpdateAsync(user);
    public Task DeleteAsync(int id) => _users.DeleteAsync(id);
    public Task<bool> ExistsAsync(int id) => _users.ExistsAsync(id);
}

public class DriverService : IDriverService
{
    private readonly IDriverRepository _drivers;

    public DriverService(IDriverRepository drivers)
    {
        _drivers = drivers;
    }

    public Task<DriverProfile?> GetByIdAsync(int id) => _drivers.GetByIdAsync(id);
    public Task<DriverProfile?> GetByUserIdAsync(int userId) => _drivers.GetByUserIdAsync(userId);
    public Task<IEnumerable<DriverProfile>> GetAllAsync() => _drivers.GetAllAsync();
    public Task<IEnumerable<DriverProfile>> GetAvailableDriversAsync() => _drivers.GetAvailableDriversAsync();
    public Task<DriverProfile> CreateAsync(DriverProfile driver) => _drivers.CreateAsync(driver);
    public Task<DriverProfile> UpdateAsync(DriverProfile driver) => _drivers.UpdateAsync(driver);
}
public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratings;
    private readonly IRideRepository _rides;
    private readonly IDriverRepository _drivers;

    public RatingService(
        IRatingRepository ratings,
        IRideRepository rides,
        IDriverRepository drivers)
    {
        _ratings = ratings;
        _rides = rides;
        _drivers = drivers;
    }

    public async Task<RatingDto> CreateRatingAsync(int passengerId, CreateRatingDto dto)
    {
        if (dto.Score < 1 || dto.Score > 5)
            throw new InvalidOperationException("Score must be between 1 and 5.");

        var ride = await _rides.GetByIdAsync(dto.RideId);
        if (ride == null)
            throw new KeyNotFoundException("Ride not found.");

        if (ride.PassengerId != passengerId)
            throw new UnauthorizedAccessException();

        if (ride.Status != RideStatus.Completed)
            throw new InvalidOperationException("Can only rate completed rides.");

        var existing = await _ratings.GetByRideIdAsync(dto.RideId);
        if (existing != null)
            throw new InvalidOperationException("Ride already rated.");

        var rating = new RideRating
        {
            RideId = dto.RideId,
            RaterId = passengerId,
            Score = dto.Score,
            Comment = dto.Comment
        };

        var created = await _ratings.CreateAsync(rating);

        if (ride.DriverProfileId.HasValue)
        {
            var driver = await _drivers.GetByIdAsync(ride.DriverProfileId.Value);

            if (driver != null)
            {
                var allRatings = (await _ratings.GetByDriverAsync(driver.Id)).ToList();

                driver.AverageRating = Math.Round(allRatings.Average(r => r.Score), 2);
                driver.Rating1Count = allRatings.Count(r => r.Score == 1);
                driver.Rating2Count = allRatings.Count(r => r.Score == 2);
                driver.Rating3Count = allRatings.Count(r => r.Score == 3);
                driver.Rating4Count = allRatings.Count(r => r.Score == 4);
                driver.Rating5Count = allRatings.Count(r => r.Score == 5);

                await _drivers.UpdateAsync(driver);
            }
        }

        return new RatingDto(
            created.Id,
            created.RideId,
            "",
            created.Score,
            created.Comment,
            created.CreatedAt
        );
    }

    public async Task<IEnumerable<RatingDto>> GetDriverRatingsAsync(int driverProfileId)
    {
        var ratings = await _ratings.GetByDriverAsync(driverProfileId);

        return ratings.Select(r => new RatingDto(
            r.Id,
            r.RideId,
            r.Rater.FullName,
            r.Score,
            r.Comment,
            r.CreatedAt
        ));
    }

    public Task<RideRating?> GetByRideIdAsync(int rideId) => _ratings.GetByRideIdAsync(rideId);
    public Task<IEnumerable<RideRating>> GetByDriverAsync(int driverProfileId) => _ratings.GetByDriverAsync(driverProfileId);
    public Task<RideRating> CreateAsync(RideRating rating) => _ratings.CreateAsync(rating);
}
public class RideService : IRideService
{
    private readonly IRideRepository _rideRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly INotificationRepository _notificationRepo;

    public RideService(
        IRideRepository rideRepo,
        IDriverRepository driverRepo,
        INotificationRepository notificationRepo)
    {
        _rideRepo = rideRepo;
        _driverRepo = driverRepo;
        _notificationRepo = notificationRepo;
    }

    public async Task<Ride> RequestRideAsync(int passengerId, RequestRideDto dto)
    {
        var existing = await _rideRepo.GetActiveRideForPassengerAsync(passengerId);

        if (existing != null)
        {
            throw new InvalidOperationException("You already have an active ride.");
        }

        var distance = CalculateDistance(
            dto.PickupLatitude,
            dto.PickupLongitude,
            dto.DropoffLatitude,
            dto.DropoffLongitude
        );

        var surge = await GetSurgeMultiplierAsync();

        var fare = await CalculateFareAsync(
            dto.PickupLatitude,
            dto.PickupLongitude,
            dto.DropoffLatitude,
            dto.DropoffLongitude
        );

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
    // Tetovo bounding box (business rule)
    private static bool IsInTetovo(double lat, double lng) =>
        lat >= 41.95 && lat <= 42.05 &&
        lng >= 20.90 && lng <= 21.10;

    public async Task<object> FindDriversAsync(
        double pickupLat, double pickupLng, double dropoffLat, double dropoffLng)
    {
        if (!IsInTetovo(pickupLat, pickupLng) || !IsInTetovo(dropoffLat, dropoffLng))
            throw new InvalidOperationException("SafeRide only operates within Tetovo.");

        var available = await _driverRepo.GetAvailableDriversAsync();

        var surge = await GetSurgeMultiplierAsync();
        var fare = await CalculateFareAsync(pickupLat, pickupLng, dropoffLat, dropoffLng);
        fare = Math.Round(fare * surge, 2);
        var distance = CalculateDistance(pickupLat, pickupLng, dropoffLat, dropoffLng);

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
            DistanceToPickup = Math.Round(CalculateDistance(d.Latitude, d.Longitude, pickupLat, pickupLng), 1),
            EtaMinutes = Math.Max(1, (int)Math.Round(CalculateDistance(d.Latitude, d.Longitude, pickupLat, pickupLng) / 0.5))
        }).OrderBy(d => d.DistanceToPickup).ToList();

        return new { drivers = options, estimatedFare = fare, distanceKm = Math.Round(distance, 1), surgeMultiplier = surge };
    }

    public async Task<int> BookRideAsync(int passengerId, BookRideDto dto)
    {
        if (!IsInTetovo(dto.PickupLat, dto.PickupLng) || !IsInTetovo(dto.DropoffLat, dto.DropoffLng))
            throw new InvalidOperationException("SafeRide only operates within Tetovo.");

        var existing = await _rideRepo.GetActiveRideForPassengerAsync(passengerId);
        if (existing != null)
            throw new InvalidOperationException("You already have an active ride.");

        var surge = await GetSurgeMultiplierAsync();
        var fare = await CalculateFareAsync(dto.PickupLat, dto.PickupLng, dto.DropoffLat, dto.DropoffLng);
        fare = Math.Round(fare * surge, 2);
        var distance = CalculateDistance(dto.PickupLat, dto.PickupLng, dto.DropoffLat, dto.DropoffLng);

        var ride = new Ride
        {
            PassengerId = passengerId,
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

        var created = await _rideRepo.CreateAsync(ride);

        // Notify the chosen driver (availability stays true until they accept)
        var driver = await _driverRepo.GetByIdAsync(dto.DriverProfileId);
        if (driver != null)
        {
            await _notificationRepo.CreateAsync(new Notification
            {
                UserId = driver.UserId,
                Title = "🚗 New Ride Request",
                Message = $"A passenger chose you! {dto.PickupAddress} → {dto.DropoffAddress}. Fare: {fare} MKD."
            });
        }

        return created.Id;
    }
    public async Task<decimal> GetSurgeMultiplierAsync()
    {
        var activeRequests = await _rideRepo.CountByStatusAsync(RideStatus.Requested);
        var availableDrivers = await _driverRepo.CountAvailableApprovedAsync();

        if (availableDrivers == 0)
        {
            return 1.0m;
        }

        var ratio = (double)activeRequests / availableDrivers;

        if (ratio >= 3) return 2.0m;
        if (ratio >= 2) return 1.5m;
        if (ratio >= 1.5) return 1.2m;

        return 1.0m;
    }

    public async Task<List<DriverProfile>> GetNearbyDriversAsync(double lat, double lng, double radiusKm = 5)
    {
        var available = await _driverRepo.GetAvailableDriversAsync();

        return available
            .Where(d => CalculateDistance(d.Latitude, d.Longitude, lat, lng) <= radiusKm)
            .ToList();
    }

    public async Task<Ride> AcceptRideAsync(int rideId, int driverProfileId)
    {
        var ride = await _rideRepo.GetByIdAsync(rideId)
            ?? throw new KeyNotFoundException("Ride not found.");

        if (ride.Status != RideStatus.Requested)
        {
            throw new InvalidOperationException("Ride is not in Requested state.");
        }

        var driver = await _driverRepo.GetByIdAsync(driverProfileId)
            ?? throw new KeyNotFoundException("Driver not found.");

        if (!driver.IsAvailable || !driver.IsApproved)
        {
            throw new InvalidOperationException("Driver is not available.");
        }

        ride.DriverProfileId = driverProfileId;
        ride.Status = RideStatus.Accepted;
        ride.AcceptedAt = DateTime.UtcNow;

        driver.IsAvailable = false;
        await _driverRepo.UpdateAsync(driver);

        await _notificationRepo.CreateAsync(new Notification
        {
            UserId = ride.PassengerId,
            Title = "🚗 Driver Found!",
            Message = $"Your driver {driver.User.FullName} has accepted your ride. Look for {driver.Vehicle?.Make} {driver.Vehicle?.Model} — {driver.Vehicle?.LicensePlate}."
        });

        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<Ride> StartRideAsync(int rideId, int driverProfileId)
    {
        var ride = await _rideRepo.GetByIdAsync(rideId)
            ?? throw new KeyNotFoundException("Ride not found.");

        if (ride.Status != RideStatus.Accepted)
        {
            throw new InvalidOperationException("Ride must be Accepted before starting.");
        }

        if (ride.DriverProfileId != driverProfileId)
        {
            throw new UnauthorizedAccessException("Not your ride.");
        }

        ride.Status = RideStatus.InProgress;
        ride.StartedAt = DateTime.UtcNow;

        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<Ride> CompleteRideAsync(int rideId, int driverProfileId)
    {
        var ride = await _rideRepo.GetByIdAsync(rideId)
            ?? throw new KeyNotFoundException("Ride not found.");

        if (ride.Status != RideStatus.InProgress)
        {
            throw new InvalidOperationException("Ride must be InProgress to complete.");
        }

        if (ride.DriverProfileId != driverProfileId)
        {
            throw new UnauthorizedAccessException("Not your ride.");
        }

        ride.Status = RideStatus.Completed;
        ride.CompletedAt = DateTime.UtcNow;
        ride.FinalFare = ride.EstimatedFare;

        var driver = await _driverRepo.GetByIdAsync(driverProfileId);

        if (driver != null)
        {
            driver.IsAvailable = true;
            driver.TotalRides++;
            await _driverRepo.UpdateAsync(driver);
        }

        await _notificationRepo.CreateAsync(new Notification
        {
            UserId = ride.PassengerId,
            Title = "✅ Ride Completed",
            Message = $"Your ride is complete! Total: {ride.FinalFare} MKD. Please rate your driver."
        });

        return await _rideRepo.UpdateAsync(ride);
    }

    public async Task<Ride> CancelRideAsync(int rideId, int userId, string reason = "")
    {
        var ride = await _rideRepo.GetByIdAsync(rideId)
            ?? throw new KeyNotFoundException("Ride not found.");

        if (ride.Status == RideStatus.Completed || ride.Status == RideStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot cancel a completed or already cancelled ride.");
        }

        ride.Status = RideStatus.Cancelled;
        ride.CancellationReason = reason;

        if (ride.DriverProfileId.HasValue)
        {
            var driver = await _driverRepo.GetByIdAsync(ride.DriverProfileId.Value);

            if (driver != null)
            {
                driver.IsAvailable = true;
                await _driverRepo.UpdateAsync(driver);
            }
        }

        return await _rideRepo.UpdateAsync(ride);
    }

    public Task<decimal> CalculateFareAsync(
        double pickupLat,
        double pickupLng,
        double dropoffLat,
        double dropoffLng)
    {
        var distanceKm = CalculateDistance(pickupLat, pickupLng, dropoffLat, dropoffLng);

        decimal baseFare = 50m;
        decimal perKm = 30m;

        var fare = baseFare + (decimal)distanceKm * perKm;

        return Task.FromResult(fare);
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg)
    {
        return deg * Math.PI / 180;
    }

    public Task<Ride?> GetByIdAsync(int id) => _rideRepo.GetByIdAsync(id);
    public Task<IEnumerable<Ride>> GetAllAsync() => _rideRepo.GetAllAsync();
    public Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId) => _rideRepo.GetByPassengerIdAsync(passengerId);
    public Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId) => _rideRepo.GetByDriverProfileIdAsync(driverProfileId);
    
    public Task<Ride> UpdateAsync(Ride ride)
    {
        return _rideRepo.UpdateAsync(ride);
    }
}


public class DriverLocationService : IDriverLocationService
{
    private readonly IDriverLocationRepository _locations;

    public DriverLocationService(IDriverLocationRepository locations)
    {
        _locations = locations;
    }

    public async Task<bool> UpdateLocationAsync(int userId, double lat, double lng)
    {
        var driver = await _locations.GetDriverByUserIdAsync(userId);
        if (driver == null) return false;

        driver.Latitude = lat;
        driver.Longitude = lng;
        await _locations.UpdateDriverAsync(driver);

        var loc = await _locations.GetByDriverProfileIdAsync(driver.Id);

        if (loc == null)
        {
            await _locations.CreateAsync(new DriverLocation
            {
                DriverProfileId = driver.Id,
                Latitude = lat,
                Longitude = lng
            });
        }
        else
        {
            loc.Latitude = lat;
            loc.Longitude = lng;
            loc.UpdatedAt = DateTime.UtcNow;
            await _locations.UpdateAsync(loc);
        }

        return true;
    }

    public Task<DriverLocation?> GetLocationAsync(int driverProfileId)
    {
        return _locations.GetByDriverProfileIdAsync(driverProfileId);
    }
}

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteLocationRepository _favorites;

    public FavoriteService(IFavoriteLocationRepository favorites)
    {
        _favorites = favorites;
    }

    public Task<IEnumerable<FavoriteLocation>> GetAllAsync(int userId)
    {
        return _favorites.GetByUserIdAsync(userId);
    }

    public Task<FavoriteLocation> AddAsync(int userId, FavoriteDto dto)
    {
        return _favorites.CreateAsync(new FavoriteLocation
        {
            UserId = userId,
            Label = dto.Label,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        });
    }

    public async Task<bool> DeleteAsync(int userId, int id)
    {
        var fav = await _favorites.GetByIdAsync(id);

        if (fav == null || fav.UserId != userId)
            return false;

        await _favorites.DeleteAsync(fav);
        return true;
    }
}

public class ChatService : IChatService
{
    private readonly IChatRepository _chat;

    public ChatService(IChatRepository chat)
    {
        _chat = chat;
    }

    public Task<IEnumerable<ChatMessage>> GetMessagesAsync(int rideId, int lastId)
    {
        return _chat.GetMessagesAsync(rideId, lastId);
    }

    public async Task<ChatMessage> SendAsync(int rideId, int senderId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("Message cannot be empty.");

        return await _chat.CreateAsync(new ChatMessage
        {
            RideId = rideId,
            SenderId = senderId,
            Message = message.Trim()
        });
    }
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public Task<IEnumerable<Notification>> GetAllAsync(int userId)
    {
        return _notifications.GetByUserIdAsync(userId);
    }

    public Task<int> UnreadCountAsync(int userId)
    {
        return _notifications.UnreadCountAsync(userId);
    }

    public async Task<bool> MarkReadAsync(int userId, int id)
    {
        var notification = await _notifications.GetByIdAsync(id);

        if (notification == null || notification.UserId != userId)
            return false;

        notification.IsRead = true;
        await _notifications.UpdateAsync(notification);

        return true;
    }

    public Task<Notification> CreateAsync(Notification notification)
    {
        return _notifications.CreateAsync(notification);
    }
}

public class SosService : ISosService
{
    private readonly ISosRepository _sos;
    private readonly IUserService _users;
    private readonly INotificationService _notifications;

    public SosService(
        ISosRepository sos,
        IUserService users,
        INotificationService notifications)
    {
        _sos = sos;
        _users = users;
        _notifications = notifications;
    }

    public async Task TriggerAsync(int userId, SosTriggerDto dto)
    {
        await _sos.CreateAsync(new SosAlert
        {
            UserId = userId,
            RideId = dto.RideId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        });

        var users = await _users.GetAllAsync();
        var admins = users.Where(u => u.Role == "Admin");

        foreach (var admin in admins)
        {
            await _notifications.CreateAsync(new Notification
            {
                UserId = admin.Id,
                Title = "🚨 SOS ALERT",
                Message = $"User {userId} triggered SOS at {dto.Latitude:F4}, {dto.Longitude:F4}."
            });
        }
    }

    public Task<IEnumerable<SosAlert>> GetAlertsAsync()
    {
        return _sos.GetAllAlertsAsync();
    }

    public async Task<bool> ResolveAsync(int id)
    {
        var alert = await _sos.GetByIdAsync(id);

        if (alert == null)
            return false;

        alert.IsResolved = true;
        await _sos.UpdateAsync(alert);

        return true;
    }
}

public class AdminService : IAdminService
{
    private readonly IAdminRepository _admin;
    private readonly IDriverService _drivers;
    private readonly IUserService _users;

    public AdminService(
        IAdminRepository admin,
        IDriverService drivers,
        IUserService users)
    {
        _admin = admin;
        _drivers = drivers;
        _users = users;
    }

    public async Task<object> DashboardAsync()
    {
        var pending = await _admin.GetPendingDriversAsync();

        return new
        {
            totalUsers = await _admin.CountUsersAsync(),
            totalDrivers = await _admin.CountDriversAsync(),
            totalRides = await _admin.CountRidesAsync(),
            completedRides = await _admin.CountCompletedRidesAsync(),
            pendingDrivers = pending.Select(d => new
            {
                d.Id,
                d.UserId,
                d.LicenseNumber,
                d.IsApproved
            })
        };
    }

    public async Task<bool> ApproveDriverAsync(int driverId)
    {
        var driver = await _drivers.GetByIdAsync(driverId);

        if (driver == null)
            return false;

        driver.IsApproved = true;
        driver.IsAvailable = true;

        if (driver.User != null)
        {
            driver.User.IsVerified = true;
        }

        await _drivers.UpdateAsync(driver);
        return true;
    }

    public async Task<bool> DeactivateUserAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);

        if (user == null)
            return false;

        user.IsActive = false;
        await _users.UpdateAsync(user);

        return true;
    }
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analytics;

    public AnalyticsService(IAnalyticsRepository analytics)
    {
        _analytics = analytics;
    }

    public async Task<object> GetAnalyticsAsync()
    {
        var now = DateTime.UtcNow;
        var rides = (await _analytics.GetAllRidesWithPaymentsAsync()).ToList();

        var ridesPerDay = Enumerable.Range(0, 7).Select(i =>
        {
            var day = now.Date.AddDays(-6 + i);
            return new
            {
                date = day.ToString("dd MMM"),
                count = rides.Count(r => r.RequestedAt.Date == day)
            };
        }).ToList();

        var revenuePerDay = Enumerable.Range(0, 7).Select(i =>
        {
            var day = now.Date.AddDays(-6 + i);
            return new
            {
                date = day.ToString("dd MMM"),
                amount = rides
                    .Where(r => r.CompletedAt?.Date == day)
                    .Sum(r => r.FinalFare ?? 0)
            };
        }).ToList();

        var peakHours = Enumerable.Range(0, 24).Select(h =>
            new
            {
                hour = $"{h}:00",
                count = rides.Count(r => r.RequestedAt.Hour == h)
            }).ToList();

        var statusBreakdown = Enum.GetValues<RideStatus>().Select(s =>
            new
            {
                status = s.ToString(),
                count = rides.Count(r => r.Status == s)
            }).ToList();

        return new
        {
            ridesPerDay,
            revenuePerDay,
            peakHours,
            statusBreakdown,
            totalRevenue = rides
                .Where(r => r.Status == RideStatus.Completed)
                .Sum(r => r.FinalFare ?? 0),
            totalRides = rides.Count,
            completionRate = rides.Any()
                ? Math.Round((double)rides.Count(r => r.Status == RideStatus.Completed) / rides.Count * 100, 1)
                : 0,
            activeDrivers = await _analytics.CountActiveDriversAsync()
        };
    }
}

public class EarningsService : IEarningsService
{
    private readonly IEarningsRepository _earnings;

    public EarningsService(IEarningsRepository earnings)
    {
        _earnings = earnings;
    }

    public async Task<object?> GetEarningsAsync(int userId)
    {
        var driver = await _earnings.GetDriverByUserIdAsync(userId);

        if (driver == null)
            return null;

        var completed = (await _earnings.GetCompletedRidesByDriverAsync(driver.Id)).ToList();

        var today = DateTime.UtcNow.Date;
        var thisWeek = today.AddDays(-(int)today.DayOfWeek);
        var thisMonth = new DateTime(today.Year, today.Month, 1);

        return new
        {
            totalEarnings = completed.Sum(r => r.FinalFare ?? 0),
            todayEarnings = completed
                .Where(r => r.CompletedAt?.Date == today)
                .Sum(r => r.FinalFare ?? 0),
            weekEarnings = completed
                .Where(r => r.CompletedAt >= thisWeek)
                .Sum(r => r.FinalFare ?? 0),
            monthEarnings = completed
                .Where(r => r.CompletedAt >= thisMonth)
                .Sum(r => r.FinalFare ?? 0),
            rides = completed.Select(r => new
            {
                r.Id,
                r.PickupAddress,
                r.DropoffAddress,
                r.FinalFare,
                r.CompletedAt
            })
        };
    }
}

public class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receipts;

    public ReceiptService(IReceiptRepository receipts)
    {
        _receipts = receipts;
    }

    public async Task<object?> GetReceiptAsync(int rideId, int userId, string role)
    {
        var ride = await _receipts.GetReceiptRideAsync(rideId);

        if (ride == null)
            return null;

        if (ride.PassengerId != userId &&
            ride.DriverProfile?.UserId != userId &&
            role != "Admin")
        {
            throw new UnauthorizedAccessException();
        }

        var duration = ride.StartedAt.HasValue && ride.CompletedAt.HasValue
            ? (int)(ride.CompletedAt.Value - ride.StartedAt.Value).TotalMinutes
            : 0;

        return new
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
        };
    }
}

public class PaymentService : IPaymentService
{
    private readonly IRideService _rides;
    private readonly IPaymentRepository _payments;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _config;

    public PaymentService(
        IRideService rides,
        IPaymentRepository payments,
        INotificationService notifications,
        IConfiguration config)
    {
        _rides = rides;
        _payments = payments;
        _notifications = notifications;
        _config = config;
    }

    public async Task<object?> CreateCardIntentAsync(int rideId, int userId)
    {
        var ride = await _rides.GetByIdAsync(rideId);

        if (ride == null || ride.PassengerId != userId)
            return null;

        Stripe.StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

        var options = new Stripe.PaymentIntentCreateOptions
        {
            Amount = (long)(ride.EstimatedFare * 100),
            Currency = "mkd",
            AutomaticPaymentMethods = new Stripe.PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            }
        };

        var service = new Stripe.PaymentIntentService();
        var intent = await service.CreateAsync(options);

        await _payments.CreateAsync(new Payment
        {
            RideId = rideId,
            Method = "Card",
            Amount = ride.EstimatedFare,
            StripePaymentIntentId = intent.Id
        });

        ride.PaymentMethod = "Card";
        await _rides.UpdateAsync(ride);

        return new { clientSecret = intent.ClientSecret };
    }

    public async Task<bool> CashPaymentAsync(int rideId, int userId)
    {
        var ride = await _rides.GetByIdAsync(rideId);

        if (ride == null || ride.PassengerId != userId)
            return false;

        await _payments.CreateAsync(new Payment
        {
            RideId = rideId,
            Method = "Cash",
            Amount = ride.FinalFare ?? ride.EstimatedFare,
            Status = "Completed"
        });

        if (ride.DriverProfile != null)
        {
            await _notifications.CreateAsync(new Notification
            {
                UserId = ride.DriverProfile.UserId,
                Title = "💵 Cash Payment Received",
                Message = $"Passenger paid {(ride.FinalFare ?? ride.EstimatedFare):F2} MKD cash for ride #{rideId}."
            });
        }

        return true;
    }
}

