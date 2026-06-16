namespace SafeRide.API.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = "Passenger"; // Admin, Driver, Passenger
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DriverProfile? DriverProfile { get; set; }
    public ICollection<Ride> RidesAsPassenger { get; set; } = new List<Ride>();
    public ICollection<RideRating> RatingsGiven { get; set; } = new List<RideRating>();
}
