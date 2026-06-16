namespace SafeRide.API.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SosAlert
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int? RideId { get; set; }
    public Ride? Ride { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsResolved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Payment
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;
    public string Method { get; set; } = "Cash";
    public string Status { get; set; } = "Pending";
    public decimal Amount { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FavoriteLocation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DriverLocation
{
    public int Id { get; set; }
    public int DriverProfileId { get; set; }
    public DriverProfile DriverProfile { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class RideReceipt
{
    public int RideId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string VehicleInfo { get; set; } = string.Empty;
    public string PickupAddress { get; set; } = string.Empty;
    public string DropoffAddress { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public decimal BaseFare { get; set; }
    public decimal DistanceFare { get; set; }
    public decimal SurgeMultiplier { get; set; }
    public decimal TotalFare { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public int DurationMinutes { get; set; }
}
