namespace SafeRide.API.Models;

public class Vehicle
{
    public int Id { get; set; }
    public int DriverProfileId { get; set; }
    public DriverProfile DriverProfile { get; set; } = null!;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public int Year { get; set; }
}

public enum RideStatus
{
    Requested,
    Accepted,
    InProgress,
    Completed,
    Cancelled
}

public class Ride
{
    public int Id { get; set; }
    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;
    public int? DriverProfileId { get; set; }
    public DriverProfile? DriverProfile { get; set; }

    public string PickupAddress { get; set; } = string.Empty;
    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }
    public string DropoffAddress { get; set; } = string.Empty;
    public double DropoffLatitude { get; set; }
    public double DropoffLongitude { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Requested;
    public decimal EstimatedFare { get; set; }
    public decimal? FinalFare { get; set; }
    public double? DistanceKm { get; set; }
    public decimal SurgeMultiplier { get; set; } = 1.0m;
    public string? CancellationReason { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledFor { get; set; }
    public bool IsScheduled { get; set; } = false;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string PaymentMethod { get; set; } = "Cash";

    public RideRating? Rating { get; set; }
    public Payment? Payment { get; set; }
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

public class RideRating
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;
    public int RaterId { get; set; }
    public User Rater { get; set; } = null!;
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
