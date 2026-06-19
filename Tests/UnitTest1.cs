using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;
using SafeRide.API.Repositories;
using SafeRide.API.Controllers;
using SafeRide.API.Services;
using SafeRide.API.DTOs;
using System.Security.Claims;

namespace SafeRide.Tests;

static class TestHelpers
{
    public static AppDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Db_" + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }

    public static ClaimsPrincipal MakePrincipal(int id, string role) =>
        new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, $"user{id}@test.com")
        }, "Test"));

    public static User MakeUserModel(int id, string role, string name = "Test User") => new User
    {
        Id = id,
        FullName = name,
        Email = $"user{id}@test.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
        PhoneNumber = "070000000",
        Role = role,
        IsActive = true,
        IsVerified = role == "Passenger"
    };

    public static async Task<DriverProfile> SeedDriver(AppDbContext db, int driverId, int userId,
        bool approved = true, bool available = true)
    {
        var user = MakeUserModel(userId, "Driver", "Driver " + driverId);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var driver = new DriverProfile
        {
            Id = driverId,
            UserId = userId,
            IsApproved = approved,
            IsAvailable = available,
            LicenseNumber = "LIC-00" + driverId,
            Vehicle = new Vehicle
            {
                Make = "Toyota",
                Model = "Corolla",
                Color = "White",
                LicensePlate = "SK-00" + driverId + "-AB",
                Year = 2022
            }
        };
        db.DriverProfiles.Add(driver);
        await db.SaveChangesAsync();
        return driver;
    }

    public static async Task<Ride> SeedRide(AppDbContext db, int rideId, int passengerId,
        RideStatus status = RideStatus.Requested, int? driverProfileId = null, decimal fare = 100m)
    {
        if (!db.Users.Any(u => u.Id == passengerId))
        {
            db.Users.Add(MakeUserModel(passengerId, "Passenger", "Passenger " + passengerId));
            await db.SaveChangesAsync();
        }

        var ride = new Ride
        {
            Id = rideId,
            PassengerId = passengerId,
            DriverProfileId = driverProfileId,
            Status = status,
            EstimatedFare = fare,
            FinalFare = status == RideStatus.Completed ? fare : null,
            PickupAddress = "City Center (Qendra)",
            DropoffAddress = "Tetovo Hospital",
            PickupLatitude = 41.9981,
            PickupLongitude = 20.9716,
            DropoffLatitude = 42.0010,
            DropoffLongitude = 20.9680,
            DistanceKm = 0.5,
            SurgeMultiplier = 1.0m,
            RequestedAt = DateTime.UtcNow,
            StartedAt = (status == RideStatus.InProgress || status == RideStatus.Completed) ? DateTime.UtcNow.AddMinutes(-10) : null,
            CompletedAt = status == RideStatus.Completed ? DateTime.UtcNow : null
        };
        db.Rides.Add(ride);
        await db.SaveChangesAsync();
        return ride;
    }

    public static Ride MakeRide(int id, int passengerId, RideStatus status = RideStatus.Requested,
        int? driverProfileId = null, decimal fare = 100m) => new Ride
        {
            Id = id,
            PassengerId = passengerId,
            DriverProfileId = driverProfileId,
            Status = status,
            EstimatedFare = fare,
            FinalFare = status == RideStatus.Completed ? fare : null,
            PickupAddress = "City Center (Qendra)",
            DropoffAddress = "Tetovo Hospital",
            PickupLatitude = 41.9981,
            PickupLongitude = 20.9716,
            DropoffLatitude = 42.0010,
            DropoffLongitude = 20.9680,
            DistanceKm = 0.5,
            SurgeMultiplier = 1.0m,
            RequestedAt = DateTime.UtcNow,
            StartedAt = (status == RideStatus.InProgress || status == RideStatus.Completed) ? DateTime.UtcNow.AddMinutes(-10) : null,
            CompletedAt = status == RideStatus.Completed ? DateTime.UtcNow : null
        };

    public static DriverProfile MakeDriverProfile(int id, int userId,
        bool approved = true, bool available = true) => new DriverProfile
        {
            Id = id,
            UserId = userId,
            IsApproved = approved,
            IsAvailable = available,
            LicenseNumber = "LIC-00" + id,
            User = MakeUserModel(userId, "Driver", "Driver " + id),
            Vehicle = new Vehicle
            {
                Id = id,
                Make = "Toyota",
                Model = "Corolla",
                Color = "White",
                LicensePlate = "SK-00" + id + "-AB",
                Year = 2022
            }
        };

    public static IConfiguration MakeConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyThatIsLongEnoughForHmacSha256!",
                ["Jwt:Issuer"] = "SafeRideTests",
                ["Jwt:Audience"] = "SafeRideTestClient"
            })
            .Build();
}

public class RideRepositoryTests
{
    [Fact]
    public async Task CreateAsync_SavesRide()
    {
        var db = TestHelpers.GetDb();
        var ride = await TestHelpers.SeedRide(db, 0, 1);
        Assert.True(ride.Id > 0);
        Assert.Equal(1, await db.Rides.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await new RideRepository(TestHelpers.GetDb()).GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRide_WhenFound()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 1);
        var result = await new RideRepository(db).GetByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.PassengerId);
    }

    [Fact]
    public async Task GetActiveRideForPassengerAsync_ReturnsActiveRide()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 5, RideStatus.InProgress);
        var result = await new RideRepository(db).GetActiveRideForPassengerAsync(5);
        Assert.NotNull(result);
        Assert.Equal(RideStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task GetActiveRideForPassengerAsync_ReturnsNull_WhenNoActive()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 5, RideStatus.Completed);
        Assert.Null(await new RideRepository(db).GetActiveRideForPassengerAsync(5));
    }

    [Fact]
    public async Task GetByPassengerIdAsync_ReturnsOnlyPassengerRides()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 1, RideStatus.Completed);
        await TestHelpers.SeedRide(db, 2, 2, RideStatus.Completed);
        await TestHelpers.SeedRide(db, 3, 1, RideStatus.Cancelled);
        var result = await new RideRepository(db).GetByPassengerIdAsync(1);
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(1, r.PassengerId));
    }

    [Fact]
    public async Task GetByDriverProfileIdAsync_ReturnsOnlyDriverRides()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 1, RideStatus.Completed, driverProfileId: 10);
        await TestHelpers.SeedRide(db, 2, 2, RideStatus.Completed, driverProfileId: 99);
        await TestHelpers.SeedRide(db, 3, 3, RideStatus.Completed, driverProfileId: 10);
        var result = await new RideRepository(db).GetByDriverProfileIdAsync(10);
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(10, r.DriverProfileId));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRides()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 1);
        await TestHelpers.SeedRide(db, 2, 2);
        await TestHelpers.SeedRide(db, 3, 3);
        var result = await new RideRepository(db).GetAllAsync();
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task CountByStatusAsync_ReturnsCorrectCount()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 1, RideStatus.Requested);
        await TestHelpers.SeedRide(db, 2, 2, RideStatus.Requested);
        await TestHelpers.SeedRide(db, 3, 3, RideStatus.Completed);
        var count = await new RideRepository(db).CountByStatusAsync(RideStatus.Requested);
        Assert.Equal(2, count);
    }
}

public class UserRepositoryTests
{
    [Fact]
    public async Task CreateAsync_SavesUser()
    {
        var db = TestHelpers.GetDb();
        var result = await new UserRepository(db).CreateAsync(TestHelpers.MakeUserModel(0, "Passenger"));
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        var db = TestHelpers.GetDb();
        var user = TestHelpers.MakeUserModel(1, "Passenger");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var result = await new UserRepository(db).GetByEmailAsync(user.Email);
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await new UserRepository(TestHelpers.GetDb()).GetByEmailAsync("nobody@test.com"));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await new UserRepository(TestHelpers.GetDb()).GetByIdAsync(9999));
    }

    [Fact]
    public async Task UpdateAsync_ChangesUserFields()
    {
        var db = TestHelpers.GetDb();
        var user = TestHelpers.MakeUserModel(1, "Passenger");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        user.FullName = "Updated Name";
        var updated = await new UserRepository(db).UpdateAsync(user);
        Assert.Equal("Updated Name", updated.FullName);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenUserExists()
    {
        var db = TestHelpers.GetDb();
        db.Users.Add(TestHelpers.MakeUserModel(1, "Passenger"));
        await db.SaveChangesAsync();
        Assert.True(await new UserRepository(db).ExistsAsync(1));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenUserMissing()
    {
        Assert.False(await new UserRepository(TestHelpers.GetDb()).ExistsAsync(999));
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var db = TestHelpers.GetDb();
        db.Users.Add(TestHelpers.MakeUserModel(1, "Passenger"));
        await db.SaveChangesAsync();
        await new UserRepository(db).DeleteAsync(1);
        Assert.Equal(0, await db.Users.CountAsync());
    }
}

public class DriverRepositoryTests
{
    [Fact]
    public async Task CreateAsync_SavesDriverProfile()
    {
        var db = TestHelpers.GetDb();
        var driver = await TestHelpers.SeedDriver(db, 0, 1);
        Assert.True(driver.Id >= 0);
        Assert.Equal(1, await db.DriverProfiles.CountAsync());
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsCorrectDriver()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedDriver(db, 1, 1);
        var result = await new DriverRepository(db).GetByUserIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNull_WhenNotFound()
    {
        Assert.Null(await new DriverRepository(TestHelpers.GetDb()).GetByUserIdAsync(999));
    }

    [Fact]
    public async Task GetAvailableDriversAsync_ReturnsOnlyAvailableAndApproved()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedDriver(db, 1, 1, approved: true, available: true);
        await TestHelpers.SeedDriver(db, 2, 2, approved: true, available: false);
        await TestHelpers.SeedDriver(db, 3, 3, approved: false, available: true);
        var result = await new DriverRepository(db).GetAvailableDriversAsync();
        Assert.Single(result);
        Assert.Equal(1, result.First().Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDrivers()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedDriver(db, 1, 1);
        await TestHelpers.SeedDriver(db, 2, 2);
        var result = await new DriverRepository(db).GetAllAsync();
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task CountAvailableApprovedAsync_ReturnsCorrectCount()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedDriver(db, 1, 1, approved: true, available: true);
        await TestHelpers.SeedDriver(db, 2, 2, approved: true, available: false);
        await TestHelpers.SeedDriver(db, 3, 3, approved: false, available: true);
        var count = await new DriverRepository(db).CountAvailableApprovedAsync();
        Assert.Equal(1, count);
    }
}

public class RatingRepositoryTests
{
    [Fact]
    public async Task CreateAsync_SavesRating()
    {
        var db = TestHelpers.GetDb();
        var rating = new RideRating { RideId = 1, RaterId = 1, Score = 5 };
        var result = await new RatingRepository(db).CreateAsync(rating);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task GetByRideIdAsync_ReturnsRating_WhenExists()
    {
        var db = TestHelpers.GetDb();
        db.Users.Add(TestHelpers.MakeUserModel(1, "Passenger"));
        db.RideRatings.Add(new RideRating { Id = 1, RideId = 1, RaterId = 1, Score = 4 });
        await db.SaveChangesAsync();
        var result = await new RatingRepository(db).GetByRideIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal(4, result.Score);
    }

    [Fact]
    public async Task GetByRideIdAsync_ReturnsNull_WhenNotRated()
    {
        Assert.Null(await new RatingRepository(TestHelpers.GetDb()).GetByRideIdAsync(999));
    }

    [Fact]
    public async Task GetByDriverAsync_ReturnsAllDriverRatings()
    {
        var db = TestHelpers.GetDb();
        await TestHelpers.SeedRide(db, 1, 1, RideStatus.Completed, driverProfileId: 10);
        await TestHelpers.SeedRide(db, 2, 2, RideStatus.Completed, driverProfileId: 10);
        await TestHelpers.SeedRide(db, 3, 3, RideStatus.Completed, driverProfileId: 99);
        db.Users.Add(TestHelpers.MakeUserModel(10, "Passenger", "Rater10"));
        db.Users.Add(TestHelpers.MakeUserModel(20, "Passenger", "Rater20"));
        await db.SaveChangesAsync();
        db.RideRatings.AddRange(
            new RideRating { RideId = 1, RaterId = 10, Score = 5 },
            new RideRating { RideId = 2, RaterId = 20, Score = 4 },
            new RideRating { RideId = 3, RaterId = 10, Score = 3 });
        await db.SaveChangesAsync();
        var result = await new RatingRepository(db).GetByDriverAsync(10);
        Assert.Equal(2, result.Count());
    }
}

public class RideServiceTests
{
    private readonly Mock<IRideRepository> _rideRepo = new();
    private readonly Mock<IDriverRepository> _driverRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();

    private RideService MakeService()
    {
        _rideRepo.Setup(r => r.CountByStatusAsync(It.IsAny<RideStatus>())).ReturnsAsync(0);
        _driverRepo.Setup(d => d.CountAvailableApprovedAsync()).ReturnsAsync(1);
        _notificationRepo.Setup(n => n.CreateAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);
        return new RideService(_rideRepo.Object, _driverRepo.Object, _notificationRepo.Object);
    }

    [Fact]
    public async Task RequestRide_ThrowsIfPassengerHasActiveRide()
    {
        _rideRepo.Setup(r => r.GetActiveRideForPassengerAsync(1))
            .ReturnsAsync(new Ride { Id = 99, Status = RideStatus.InProgress });
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeService().RequestRideAsync(1, new RequestRideDto("A", 0, 0, "B", 1, 1)));
    }

    [Fact]
    public async Task RequestRide_CreatesRide_WhenNoActiveRide()
    {
        _rideRepo.Setup(r => r.GetActiveRideForPassengerAsync(1)).ReturnsAsync((Ride?)null);
        _rideRepo.Setup(r => r.CreateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => { r.Id = 1; return r; });
        var dto = new RequestRideDto("City Center", 41.9981, 20.9716, "Hospital", 42.0010, 20.9680);
        var result = await MakeService().RequestRideAsync(1, dto);
        Assert.Equal(RideStatus.Requested, result.Status);
        Assert.Equal(1, result.PassengerId);
        Assert.True(result.EstimatedFare > 0);
    }

    [Fact]
    public async Task RequestRide_SetsCorrectAddresses()
    {
        _rideRepo.Setup(r => r.GetActiveRideForPassengerAsync(1)).ReturnsAsync((Ride?)null);
        _rideRepo.Setup(r => r.CreateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => { r.Id = 1; return r; });
        var dto = new RequestRideDto("Pickup Place", 41.9981, 20.9716, "Dropoff Place", 42.001, 20.968);
        var result = await MakeService().RequestRideAsync(1, dto);
        Assert.Equal("Pickup Place", result.PickupAddress);
        Assert.Equal("Dropoff Place", result.DropoffAddress);
    }

    [Fact]
    public async Task AcceptRide_ThrowsIfRideNotFound()
    {
        _rideRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Ride?)null);
        await Assert.ThrowsAnyAsync<Exception>(() => MakeService().AcceptRideAsync(99, 1));
    }

    [Fact]
    public async Task AcceptRide_ThrowsIfRideNotRequested()
    {
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Ride { Id = 1, Status = RideStatus.Accepted });
        await Assert.ThrowsAnyAsync<Exception>(() => MakeService().AcceptRideAsync(1, 1));
    }

    [Fact]
    public async Task AcceptRide_SetsStatusToAccepted()
    {
        var ride = TestHelpers.MakeRide(1, 1, RideStatus.Requested);
        var driver = TestHelpers.MakeDriverProfile(1, 10);
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ride);
        _driverRepo.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(driver);
        _rideRepo.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        _driverRepo.Setup(d => d.UpdateAsync(It.IsAny<DriverProfile>())).ReturnsAsync((DriverProfile d) => d);
        var result = await MakeService().AcceptRideAsync(1, 1);
        Assert.Equal(RideStatus.Accepted, result.Status);
        Assert.Equal(1, result.DriverProfileId);
        Assert.False(driver.IsAvailable);
    }

    [Fact]
    public async Task StartRide_ThrowsIfNotAccepted()
    {
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Ride { Id = 1, Status = RideStatus.Requested });
        await Assert.ThrowsAnyAsync<Exception>(() => MakeService().StartRideAsync(1, 1));
    }

    [Fact]
    public async Task StartRide_SetsStatusToInProgress()
    {
        var ride = TestHelpers.MakeRide(1, 1, RideStatus.Accepted, driverProfileId: 1);
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ride);
        _rideRepo.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        var result = await MakeService().StartRideAsync(1, 1);
        Assert.Equal(RideStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task CompleteRide_ThrowsIfNotInProgress()
    {
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Ride { Id = 1, Status = RideStatus.Requested });
        await Assert.ThrowsAnyAsync<Exception>(() => MakeService().CompleteRideAsync(1, 1));
    }

    [Fact]
    public async Task CompleteRide_SetsStatusAndFreesDriver()
    {
        var driver = TestHelpers.MakeDriverProfile(1, 10, available: false);
        driver.TotalRides = 5;
        var ride = TestHelpers.MakeRide(1, 1, RideStatus.InProgress, driverProfileId: 1, fare: 120m);
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ride);
        _driverRepo.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(driver);
        _rideRepo.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        _driverRepo.Setup(d => d.UpdateAsync(It.IsAny<DriverProfile>())).ReturnsAsync((DriverProfile d) => d);
        var result = await MakeService().CompleteRideAsync(1, 1);
        Assert.Equal(RideStatus.Completed, result.Status);
        Assert.True(driver.IsAvailable);
        Assert.Equal(6, driver.TotalRides);
        Assert.NotNull(result.FinalFare);
    }

    [Fact]
    public async Task CancelRide_ThrowsIfAlreadyCompleted()
    {
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Ride { Id = 1, Status = RideStatus.Completed });
        await Assert.ThrowsAnyAsync<Exception>(() => MakeService().CancelRideAsync(1, 1));
    }

    [Fact]
    public async Task CancelRide_SetsStatusToCancelled()
    {
        var ride = TestHelpers.MakeRide(1, 1, RideStatus.Requested);
        _rideRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ride);
        _rideRepo.Setup(r => r.UpdateAsync(It.IsAny<Ride>())).ReturnsAsync((Ride r) => r);
        var result = await MakeService().CancelRideAsync(1, 1);
        Assert.Equal(RideStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CalculateFare_ReturnsAtLeastBaseFare()
    {
        var fare = await MakeService().CalculateFareAsync(41.9981, 20.9716, 41.9981, 20.9716);
        Assert.True(fare >= 50m);
    }

    [Fact]
    public async Task CalculateFare_IncreasesWithDistance()
    {
        var svc = MakeService();
        var shortFare = await svc.CalculateFareAsync(41.9981, 20.9716, 42.0010, 20.9680);
        var longFare = await svc.CalculateFareAsync(41.9781, 20.9516, 42.0210, 20.9880);
        Assert.True(longFare > shortFare);
    }

    [Fact]
    public void CalculateDistance_ReturnsZero_ForSamePoint()
    {
        var dist = RideService.CalculateDistance(41.9981, 20.9716, 41.9981, 20.9716);
        Assert.Equal(0.0, dist, precision: 5);
    }

    [Fact]
    public void CalculateDistance_ReturnsPositive_ForDifferentPoints()
    {
        var dist = RideService.CalculateDistance(41.9981, 20.9716, 42.0010, 20.9680);
        Assert.True(dist > 0);
    }

    [Fact]
    public async Task GetSurgeMultiplier_ReturnsAtLeastOne()
    {
        Assert.True(await MakeService().GetSurgeMultiplierAsync() >= 1.0m);
    }
}

public class AuthControllerTests
{
    private AuthController MakeController(AppDbContext db)
    {
        var userService = new UserService(new UserRepository(db));
        var authService = new AuthService(TestHelpers.MakeConfig());
        return new AuthController(userService, authService);
    }

    [Fact]
    public async Task Register_ReturnsOk_WithValidPassengerData()
    {
        var result = await MakeController(TestHelpers.GetDb())
            .Register(new RegisterDto("Arta Test", "arta@test.com", "Pass123!", "070111222", "Passenger"));
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsOk_WithValidDriverData()
    {
        var result = await MakeController(TestHelpers.GetDb())
            .Register(new RegisterDto("Jona Driver", "jona@test.com", "Pass123!", "070222333", "Driver"));
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailAlreadyExists()
    {
        var db = TestHelpers.GetDb();
        var ctrl = MakeController(db);
        await ctrl.Register(new RegisterDto("Arta", "arta@test.com", "Pass123!", "070111222", "Passenger"));
        var result = await ctrl.Register(new RegisterDto("Arta2", "arta@test.com", "Pass123!", "070111333", "Passenger"));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WithInvalidRole()
    {
        var result = await MakeController(TestHelpers.GetDb())
            .Register(new RegisterDto("Hacker", "hack@test.com", "Pass123!", "070000000", "Admin"));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_PassengerIsAutoVerified()
    {
        var db = TestHelpers.GetDb();
        await MakeController(db).Register(new RegisterDto("Arta", "arta@test.com", "Pass123!", "070111222", "Passenger"));
        Assert.True(db.Users.First().IsVerified);
    }

    [Fact]
    public async Task Register_DriverIsNotAutoVerified()
    {
        var db = TestHelpers.GetDb();
        await MakeController(db).Register(new RegisterDto("Jona", "jona@test.com", "Pass123!", "070222333", "Driver"));
        Assert.False(db.Users.First().IsVerified);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithCorrectCredentials()
    {
        var db = TestHelpers.GetDb();
        var ctrl = MakeController(db);
        await ctrl.Register(new RegisterDto("Arta", "arta@test.com", "Pass123!", "070111222", "Passenger"));
        var result = await ctrl.Login(new LoginDto("arta@test.com", "Pass123!"));
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WithWrongPassword()
    {
        var db = TestHelpers.GetDb();
        var ctrl = MakeController(db);
        await ctrl.Register(new RegisterDto("Arta", "arta@test.com", "CorrectPass!", "070111222", "Passenger"));
        var result = await ctrl.Login(new LoginDto("arta@test.com", "WrongPass!"));
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
    {
        var result = await MakeController(TestHelpers.GetDb()).Login(new LoginDto("nobody@test.com", "pass"));
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenAccountDeactivated()
    {
        var db = TestHelpers.GetDb();
        var ctrl = MakeController(db);
        await ctrl.Register(new RegisterDto("Arta", "arta@test.com", "Pass123!", "070111222", "Passenger"));
        db.Users.First().IsActive = false;
        await db.SaveChangesAsync();
        var result = await ctrl.Login(new LoginDto("arta@test.com", "Pass123!"));
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}

public class RidesControllerTests
{
    private RidesController MakeController(IRideService rideService, IDriverService driverService,
        int userId = 1, string role = "Passenger")
    {
        var ctrl = new RidesController(rideService, driverService);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = TestHelpers.MakePrincipal(userId, role) }
        };
        return ctrl;
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenRideDoesNotExist()
    {
        var rideService = new Mock<IRideService>();
        rideService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((Ride?)null);
        var result = await MakeController(rideService.Object, new Mock<IDriverService>().Object).GetById(99);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenRideExistsAndUserIsPassenger()
    {
        var rideService = new Mock<IRideService>();
        rideService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(TestHelpers.MakeRide(1, 1));
        var result = await MakeController(rideService.Object, new Mock<IDriverService>().Object, userId: 1).GetById(1);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMyRides_ReturnsOk_ForPassenger()
    {
        var rideService = new Mock<IRideService>();
        rideService.Setup(s => s.GetByPassengerIdAsync(1)).ReturnsAsync(new List<Ride>
        {
            TestHelpers.MakeRide(1, 1), TestHelpers.MakeRide(2, 1)
        });
        var result = await MakeController(rideService.Object, new Mock<IDriverService>().Object, userId: 1, role: "Passenger").GetMyRides();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetMyRides_ReturnsOk_ForDriver()
    {
        var rideService = new Mock<IRideService>();
        var driverService = new Mock<IDriverService>();
        driverService.Setup(s => s.GetByUserIdAsync(10)).ReturnsAsync(TestHelpers.MakeDriverProfile(5, 10));
        rideService.Setup(s => s.GetByDriverProfileIdAsync(5)).ReturnsAsync(new List<Ride>
        {
            TestHelpers.MakeRide(1, 1, RideStatus.Accepted, driverProfileId: 5)
        });
        var result = await MakeController(rideService.Object, driverService.Object, userId: 10, role: "Driver").GetMyRides();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RequestRide_ReturnsCreatedAtAction_WithValidDto()
    {
        var rideService = new Mock<IRideService>();
        rideService.Setup(s => s.RequestRideAsync(1, It.IsAny<RequestRideDto>()))
            .ReturnsAsync(TestHelpers.MakeRide(1, 1));
        var result = await MakeController(rideService.Object, new Mock<IDriverService>().Object)
            .RequestRide(new RequestRideDto("A", 41.99, 20.97, "B", 42.00, 20.98));
        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Accept_ReturnsBadRequest_WhenDriverNotAvailable()
    {
        var driverService = new Mock<IDriverService>();
        driverService.Setup(s => s.GetByUserIdAsync(10))
            .ReturnsAsync(TestHelpers.MakeDriverProfile(1, 10, approved: true, available: false));
        var result = await MakeController(new Mock<IRideService>().Object, driverService.Object, userId: 10, role: "Driver").Accept(1);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Cancel_ReturnsBadRequest_WhenServiceThrows()
    {
        var rideService = new Mock<IRideService>();
        rideService.Setup(s => s.CancelRideAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Already completed"));
        var result = await MakeController(rideService.Object, new Mock<IDriverService>().Object).Cancel(1);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}

public class DriversControllerTests
{
    private DriversController MakeController(IDriverService service, int userId = 1, string role = "Driver")
    {
        var ctrl = new DriversController(service);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = TestHelpers.MakePrincipal(userId, role) }
        };
        return ctrl;
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenDriverMissing()
    {
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((DriverProfile?)null);
        Assert.IsType<NotFoundResult>(await MakeController(service.Object).GetById(99));
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenProfileAlreadyExists()
    {
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByUserIdAsync(1)).ReturnsAsync(TestHelpers.MakeDriverProfile(1, 1));
        var result = await MakeController(service.Object)
            .Create(new CreateDriverProfileDto("LIC", "Toyota", "Corolla", "White", "SK-001-AB", 2022));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Approve_ReturnsNotFound_WhenDriverMissing()
    {
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((DriverProfile?)null);
        Assert.IsType<NotFoundResult>(await MakeController(service.Object, role: "Admin").Approve(99));
    }

    [Fact]
    public async Task Approve_SetsIsApprovedAvailableAndVerified()
    {
        var driver = TestHelpers.MakeDriverProfile(1, 10, approved: false, available: false);
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(driver);
        service.Setup(s => s.UpdateAsync(It.IsAny<DriverProfile>())).ReturnsAsync((DriverProfile d) => d);
        var result = await MakeController(service.Object, role: "Admin").Approve(1);
        Assert.IsType<OkObjectResult>(result);
        Assert.True(driver.IsApproved);
        Assert.True(driver.IsAvailable);
        Assert.True(driver.User.IsVerified);
    }

    [Fact]
    public async Task ToggleAvailability_ReturnsBadRequest_WhenNotApproved()
    {
        var driver = TestHelpers.MakeDriverProfile(1, 1, approved: false, available: false);
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByUserIdAsync(1)).ReturnsAsync(driver);
        Assert.IsType<BadRequestObjectResult>(await MakeController(service.Object).ToggleAvailability(true));
    }

    [Fact]
    public async Task ToggleAvailability_UpdatesToTrue_WhenApproved()
    {
        var driver = TestHelpers.MakeDriverProfile(1, 1, approved: true, available: false);
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByUserIdAsync(1)).ReturnsAsync(driver);
        service.Setup(s => s.UpdateAsync(It.IsAny<DriverProfile>())).ReturnsAsync((DriverProfile d) => d);
        var result = await MakeController(service.Object).ToggleAvailability(true);
        Assert.IsType<OkObjectResult>(result);
        Assert.True(driver.IsAvailable);
    }

    [Fact]
    public async Task ToggleAvailability_UpdatesToFalse_WhenGoingOffline()
    {
        var driver = TestHelpers.MakeDriverProfile(1, 1, approved: true, available: true);
        var service = new Mock<IDriverService>();
        service.Setup(s => s.GetByUserIdAsync(1)).ReturnsAsync(driver);
        service.Setup(s => s.UpdateAsync(It.IsAny<DriverProfile>())).ReturnsAsync((DriverProfile d) => d);
        await MakeController(service.Object).ToggleAvailability(false);
        Assert.False(driver.IsAvailable);
    }
}

public class RatingsControllerTests
{
    private RatingsController MakeController(
        IRatingService ratingService,
        int userId = 1)
    {
        var ctrl = new RatingsController(ratingService);

        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = TestHelpers.MakePrincipal(userId, "Passenger")
            }
        };

        return ctrl;
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenScoreTooHigh()
    {
        var ratingService = new Mock<IRatingService>();

        ratingService
            .Setup(s => s.CreateRatingAsync(1, It.IsAny<CreateRatingDto>()))
            .ThrowsAsync(new InvalidOperationException("Score must be between 1 and 5."));

        var result = await MakeController(ratingService.Object)
            .Create(new CreateRatingDto(1, 6, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenScoreTooLow()
    {
        var ratingService = new Mock<IRatingService>();

        ratingService
            .Setup(s => s.CreateRatingAsync(1, It.IsAny<CreateRatingDto>()))
            .ThrowsAsync(new InvalidOperationException("Score must be between 1 and 5."));

        var result = await MakeController(ratingService.Object)
            .Create(new CreateRatingDto(1, 0, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenRideNotFound()
    {
        var ratingService = new Mock<IRatingService>();

        ratingService
            .Setup(s => s.CreateRatingAsync(1, It.IsAny<CreateRatingDto>()))
            .ThrowsAsync(new KeyNotFoundException("Ride not found."));

        var result = await MakeController(ratingService.Object)
            .Create(new CreateRatingDto(99, 5, null));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenRideNotCompleted()
    {
        var ratingService = new Mock<IRatingService>();

        ratingService
            .Setup(s => s.CreateRatingAsync(1, It.IsAny<CreateRatingDto>()))
            .ThrowsAsync(new InvalidOperationException("Can only rate completed rides."));

        var result = await MakeController(ratingService.Object)
            .Create(new CreateRatingDto(1, 5, null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenAlreadyRated()
    {
        var ratingService = new Mock<IRatingService>();

        ratingService
            .Setup(s => s.CreateRatingAsync(1, It.IsAny<CreateRatingDto>()))
            .ThrowsAsync(new InvalidOperationException("Ride already rated."));

        var result = await MakeController(ratingService.Object)
            .Create(new CreateRatingDto(1, 4, "Good"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk_WithValidFirstRating()
    {
        var ratingService = new Mock<IRatingService>();

        ratingService
            .Setup(s => s.CreateRatingAsync(1, It.IsAny<CreateRatingDto>()))
            .ReturnsAsync(new RatingDto(
                1,
                1,
                "",
                5,
                "Great!",
                DateTime.UtcNow
            ));

        var result = await MakeController(ratingService.Object)
            .Create(new CreateRatingDto(1, 5, "Great!"));

        Assert.IsType<OkObjectResult>(result);
    }
}
public class ModelTests
{
    [Fact] public void User_DefaultRole_IsPassenger() => Assert.Equal("Passenger", new User().Role);
    [Fact] public void User_IsActiveByDefault() => Assert.True(new User().IsActive);
    [Fact] public void User_IsNotVerifiedByDefault() => Assert.False(new User().IsVerified);
    [Fact] public void DriverProfile_IsNotAvailableByDefault() => Assert.False(new DriverProfile().IsAvailable);
    [Fact] public void DriverProfile_IsNotApprovedByDefault() => Assert.False(new DriverProfile().IsApproved);
    [Fact] public void Ride_DefaultStatus_IsRequested() => Assert.Equal(RideStatus.Requested, new Ride().Status);
    [Fact] public void Ride_IsNotScheduledByDefault() => Assert.False(new Ride().IsScheduled);
    [Fact] public void Ride_DefaultPaymentMethod_IsCash() => Assert.Equal("Cash", new Ride().PaymentMethod);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void RideRating_ScoreIsValid_WhenBetween1And5(int score) =>
        Assert.InRange(new RideRating { Score = score }.Score, 1, 5);

    [Fact]
    public void Tetovo_BoundingBox_ContainsCityCenter()
    {
        double lat = 41.9981, lng = 20.9716;
        Assert.True(lat >= 41.95 && lat <= 42.05 && lng >= 20.90 && lng <= 21.10);
    }

    [Fact]
    public void Tetovo_BoundingBox_ContainsHospital()
    {
        double lat = 42.0010, lng = 20.9680;
        Assert.True(lat >= 41.95 && lat <= 42.05 && lng >= 20.90 && lng <= 21.10);
    }

    [Fact]
    public void Tetovo_BoundingBox_DoesNotContainSkopje()
    {
        double lat = 41.9981, lng = 21.4254;
        Assert.False(lat >= 41.95 && lat <= 42.05 && lng >= 20.90 && lng <= 21.10);
    }

    [Fact]
    public void Tetovo_BoundingBox_DoesNotContainGostivar()
    {
        double lat = 41.7963, lng = 20.9093;
        Assert.False(lat >= 41.95 && lat <= 42.05 && lng >= 20.90 && lng <= 21.10);
    }
}
