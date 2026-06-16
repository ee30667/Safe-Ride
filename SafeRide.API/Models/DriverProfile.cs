namespace SafeRide.API.Models;

public class DriverProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = false;
    public bool IsApproved { get; set; } = false;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public double AverageRating { get; set; } = 0;
    public int TotalRides { get; set; } = 0;
    public int Rating1Count { get; set; } = 0;
    public int Rating2Count { get; set; } = 0;
    public int Rating3Count { get; set; } = 0;
    public int Rating4Count { get; set; } = 0;
    public int Rating5Count { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Vehicle? Vehicle { get; set; }
    public ICollection<Ride> Rides { get; set; } = new List<Ride>();
}
