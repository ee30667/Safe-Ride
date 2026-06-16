using SafeRide.API.Models;

namespace SafeRide.API.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public interface IDriverRepository
{
    Task<DriverProfile?> GetByIdAsync(int id);
    Task<DriverProfile?> GetByUserIdAsync(int userId);
    Task<IEnumerable<DriverProfile>> GetAllAsync();
    Task<IEnumerable<DriverProfile>> GetAvailableDriversAsync();
    Task<DriverProfile> CreateAsync(DriverProfile driver);
    Task<DriverProfile> UpdateAsync(DriverProfile driver);
}

public interface IRideRepository
{
    Task<Ride?> GetByIdAsync(int id);
    Task<IEnumerable<Ride>> GetAllAsync();
    Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId);
    Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId);
    Task<Ride?> GetActiveRideForPassengerAsync(int passengerId);
    Task<Ride> CreateAsync(Ride ride);
    Task<Ride> UpdateAsync(Ride ride);
}

public interface IRatingRepository
{
    Task<RideRating?> GetByRideIdAsync(int rideId);
    Task<IEnumerable<RideRating>> GetByDriverAsync(int driverProfileId);
    Task<RideRating> CreateAsync(RideRating rating);
}

public interface IAuthService
{
    Task<string> GenerateTokenAsync(User user);
}

public interface IRideService
{
    Task<Ride> RequestRideAsync(int passengerId, DTOs.RequestRideDto dto);
    Task<Ride> AcceptRideAsync(int rideId, int driverProfileId);
    Task<Ride> StartRideAsync(int rideId, int driverProfileId);
    Task<Ride> CompleteRideAsync(int rideId, int driverProfileId);
    Task<Ride> CancelRideAsync(int rideId, int userId, string reason = "");
    Task<decimal> CalculateFareAsync(double pickupLat, double pickupLng, double dropoffLat, double dropoffLng);
    Task<decimal> GetSurgeMultiplierAsync();
    Task<List<DriverProfile>> GetNearbyDriversAsync(double lat, double lng, double radiusKm = 5);
}
