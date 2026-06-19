using SafeRide.API.Controllers;
using SafeRide.API.DTOs;
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
    Task<int> CountAvailableApprovedAsync();
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
    Task<int> CountByStatusAsync(RideStatus status);
    Task<Ride> CreateAsync(Ride ride);
    Task<Ride> UpdateAsync(Ride ride);
}

public interface IRatingRepository
{
    Task<RideRating?> GetByRideIdAsync(int rideId);
    Task<IEnumerable<RideRating>> GetByDriverAsync(int driverProfileId);
    Task<RideRating> CreateAsync(RideRating rating);
}

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<int> UnreadCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int id);
    Task<Notification> UpdateAsync(Notification notification);
}

public interface IAuthService
{
    Task<string> GenerateTokenAsync(User user);
}

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);

}

public interface IDriverService
{
    Task<DriverProfile?> GetByIdAsync(int id);
    Task<DriverProfile?> GetByUserIdAsync(int userId);
    Task<IEnumerable<DriverProfile>> GetAllAsync();
    Task<IEnumerable<DriverProfile>> GetAvailableDriversAsync();
    Task<DriverProfile> CreateAsync(DriverProfile driver);
    Task<DriverProfile> UpdateAsync(DriverProfile driver);
}

public interface IRatingService
{
    Task<RideRating?> GetByRideIdAsync(int rideId);
    Task<IEnumerable<RideRating>> GetByDriverAsync(int driverProfileId);
    Task<RideRating> CreateAsync(RideRating rating);
    Task<RatingDto> CreateRatingAsync(int passengerId, CreateRatingDto dto);
    Task<IEnumerable<RatingDto>> GetDriverRatingsAsync(int driverProfileId);
}

public interface IRideService
{
    Task<Ride> RequestRideAsync(int passengerId, RequestRideDto dto);
    Task<Ride> AcceptRideAsync(int rideId, int driverProfileId);
    Task<Ride> StartRideAsync(int rideId, int driverProfileId);
    Task<Ride> CompleteRideAsync(int rideId, int driverProfileId);
    Task<Ride> CancelRideAsync(int rideId, int userId, string reason = "");
    Task<decimal> CalculateFareAsync(double pickupLat, double pickupLng, double dropoffLat, double dropoffLng);
    Task<decimal> GetSurgeMultiplierAsync();
    Task<List<DriverProfile>> GetNearbyDriversAsync(double lat, double lng, double radiusKm = 5);
    Task<Ride?> GetByIdAsync(int id);
    Task<IEnumerable<Ride>> GetAllAsync();
    Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId);
    Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId);
    Task<Ride> UpdateAsync(Ride ride);
    Task<object> FindDriversAsync(double pickupLat, double pickupLng, double dropoffLat, double dropoffLng);
    Task<int> BookRideAsync(int passengerId, BookRideDto dto);
}
public interface IDriverLocationRepository
{
    Task<DriverProfile?> GetDriverByUserIdAsync(int userId);
    Task<DriverLocation?> GetByDriverProfileIdAsync(int driverProfileId);
    Task<DriverLocation> CreateAsync(DriverLocation location);
    Task<DriverLocation> UpdateAsync(DriverLocation location);
    Task<DriverProfile> UpdateDriverAsync(DriverProfile driver);
}

public interface IFavoriteLocationRepository
{
    Task<IEnumerable<FavoriteLocation>> GetByUserIdAsync(int userId);
    Task<FavoriteLocation?> GetByIdAsync(int id);
    Task<FavoriteLocation> CreateAsync(FavoriteLocation favorite);
    Task DeleteAsync(FavoriteLocation favorite);
}

public interface IChatRepository
{
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(int rideId, int lastId);
    Task<ChatMessage> CreateAsync(ChatMessage message);
}

public interface ISosRepository
{
    Task<SosAlert> CreateAsync(SosAlert alert);
    Task<IEnumerable<SosAlert>> GetAllAlertsAsync();
    Task<SosAlert?> GetByIdAsync(int id);
    Task<SosAlert> UpdateAsync(SosAlert alert);
}

public interface IPaymentRepository
{
    Task<Payment> CreateAsync(Payment payment);
}

public interface IAdminRepository
{
    Task<int> CountUsersAsync();
    Task<int> CountDriversAsync();
    Task<int> CountRidesAsync();
    Task<int> CountCompletedRidesAsync();
    Task<IEnumerable<DriverProfile>> GetPendingDriversAsync();
}

public interface IAnalyticsRepository
{
    Task<IEnumerable<Ride>> GetAllRidesWithPaymentsAsync();
    Task<int> CountActiveDriversAsync();
}

public interface IEarningsRepository
{
    Task<DriverProfile?> GetDriverByUserIdAsync(int userId);
    Task<IEnumerable<Ride>> GetCompletedRidesByDriverAsync(int driverProfileId);
}

public interface IReceiptRepository
{
    Task<Ride?> GetReceiptRideAsync(int rideId);
}

public interface IDriverLocationService
{
    Task<bool> UpdateLocationAsync(int userId, double lat, double lng);
    Task<DriverLocation?> GetLocationAsync(int driverProfileId);
}

public interface IFavoriteService
{
    Task<IEnumerable<FavoriteLocation>> GetAllAsync(int userId);
    Task<FavoriteLocation> AddAsync(int userId, FavoriteDto dto);
    Task<bool> DeleteAsync(int userId, int id);
}

public interface IChatService
{
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(int rideId, int lastId);
    Task<ChatMessage> SendAsync(int rideId, int senderId, string message);
}

public interface INotificationService
{
    Task<IEnumerable<Notification>> GetAllAsync(int userId);
    Task<int> UnreadCountAsync(int userId);
    Task<bool> MarkReadAsync(int userId, int id);
    Task<Notification> CreateAsync(Notification notification);
}

public interface ISosService
{
    Task TriggerAsync(int userId, SosTriggerDto dto);
    Task<IEnumerable<SosAlert>> GetAlertsAsync();
    Task<bool> ResolveAsync(int id);
}

public interface IAdminService
{
    Task<object> DashboardAsync();
    Task<bool> ApproveDriverAsync(int driverId);
    Task<bool> DeactivateUserAsync(int userId);
}

public interface IAnalyticsService
{
    Task<object> GetAnalyticsAsync();
}

public interface IEarningsService
{
    Task<object?> GetEarningsAsync(int userId);
}

public interface IReceiptService
{
    Task<object?> GetReceiptAsync(int rideId, int userId, string role);
}

public interface IPaymentService
{
    Task<object?> CreateCardIntentAsync(int rideId, int userId);
    Task<bool> CashPaymentAsync(int rideId, int userId);
}