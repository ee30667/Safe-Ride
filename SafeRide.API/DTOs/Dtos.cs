namespace SafeRide.API.DTOs;

// Auth
public record RegisterDto(string FullName, string Email, string Password, string PhoneNumber, string Role);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(string Token, string Role, string FullName, int UserId);

// User
public record UserDto(int Id, string FullName, string Email, string PhoneNumber, string Role, bool IsVerified, bool IsActive, DateTime CreatedAt);
public record UpdateUserDto(string FullName, string PhoneNumber);

// Driver
public record CreateDriverProfileDto(string LicenseNumber, string VehicleMake, string VehicleModel, string VehicleColor, string LicensePlate, int VehicleYear);
public record DriverProfileDto(int Id, int UserId, string FullName, string LicenseNumber, bool IsAvailable, bool IsApproved, double AverageRating, int TotalRides, VehicleDto? Vehicle);
public record UpdateLocationDto(double Latitude, double Longitude);

// Vehicle
public record VehicleDto(int Id, string Make, string Model, string Color, string LicensePlate, int Year);

// Ride
public record RequestRideDto(string PickupAddress, double PickupLatitude, double PickupLongitude, string DropoffAddress, double DropoffLatitude, double DropoffLongitude);
public record RideDto(int Id, int PassengerId, string PassengerName, int? DriverProfileId, string? DriverName, string PickupAddress, string DropoffAddress, string Status, decimal EstimatedFare, decimal? FinalFare, double? DistanceKm, DateTime RequestedAt, DateTime? CompletedAt);
public record UpdateRideStatusDto(string Status);

// Rating
public record CreateRatingDto(int RideId, int Score, string? Comment);
public record RatingDto(int Id, int RideId, string RaterName, int Score, string? Comment, DateTime CreatedAt);
