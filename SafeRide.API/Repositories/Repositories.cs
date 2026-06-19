using Microsoft.EntityFrameworkCore;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;

namespace SafeRide.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.DriverProfile)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.DriverProfile)
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLower();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(int id)
    {
        var user = await GetByIdAsync(id);

        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }
}

public class DriverRepository : IDriverRepository
{
    private readonly AppDbContext _context;

    public DriverRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DriverProfile?> GetByIdAsync(int id)
    {
        return await _context.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DriverProfile?> GetByUserIdAsync(int userId)
    {
        return await _context.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .FirstOrDefaultAsync(d => d.UserId == userId);
    }

    public async Task<IEnumerable<DriverProfile>> GetAllAsync()
    {
        return await _context.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .ToListAsync();
    }

    public async Task<IEnumerable<DriverProfile>> GetAvailableDriversAsync()
    {
        return await _context.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .Where(d => d.IsAvailable && d.IsApproved && d.User.IsActive)
            .ToListAsync();
    }

    public async Task<int> CountAvailableApprovedAsync()
    {
        return await _context.DriverProfiles
            .CountAsync(d => d.IsAvailable && d.IsApproved);
    }

    public async Task<DriverProfile> CreateAsync(DriverProfile driver)
    {
        _context.DriverProfiles.Add(driver);
        await _context.SaveChangesAsync();
        return driver;
    }

    public async Task<DriverProfile> UpdateAsync(DriverProfile driver)
    {
        _context.DriverProfiles.Update(driver);
        await _context.SaveChangesAsync();
        return driver;
    }
}

public class RideRepository : IRideRepository
{
    private readonly AppDbContext _context;

    public RideRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<Ride> FullQuery()
    {
        return _context.Rides
            .Include(r => r.Passenger)
            .Include(r => r.DriverProfile)
                .ThenInclude(d => d!.User)
            .Include(r => r.Rating);
    }

    public async Task<Ride?> GetByIdAsync(int id)
    {
        return await FullQuery().FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Ride>> GetAllAsync()
    {
        return await FullQuery()
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId)
    {
        return await FullQuery()
            .Where(r => r.PassengerId == passengerId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId)
    {
        return await FullQuery()
            .Where(r => r.DriverProfileId == driverProfileId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<Ride?> GetActiveRideForPassengerAsync(int passengerId)
    {
        return await FullQuery()
            .FirstOrDefaultAsync(r =>
                r.PassengerId == passengerId &&
                (r.Status == RideStatus.Requested ||
                 r.Status == RideStatus.Accepted ||
                 r.Status == RideStatus.InProgress));
    }

    public async Task<int> CountByStatusAsync(RideStatus status)
    {
        return await _context.Rides.CountAsync(r => r.Status == status);
    }

    public async Task<Ride> CreateAsync(Ride ride)
    {
        _context.Rides.Add(ride);
        await _context.SaveChangesAsync();
        return ride;
    }

    public async Task<Ride> UpdateAsync(Ride ride)
    {
        _context.Rides.Update(ride);
        await _context.SaveChangesAsync();
        return ride;
    }
}

public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _context;

    public RatingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RideRating?> GetByRideIdAsync(int rideId)
    {
        return await _context.RideRatings
            .Include(r => r.Rater)
            .FirstOrDefaultAsync(r => r.RideId == rideId);
    }

    public async Task<IEnumerable<RideRating>> GetByDriverAsync(int driverProfileId)
    {
        return await _context.RideRatings
            .Include(r => r.Rater)
            .Include(r => r.Ride)
            .Where(r => r.Ride.DriverProfileId == driverProfileId)
            .ToListAsync();
    }

    public async Task<RideRating> CreateAsync(RideRating rating)
    {
        _context.RideRatings.Add(rating);
        await _context.SaveChangesAsync();
        return rating;
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> UnreadCountAsync(int userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task<Notification> UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
        return notification;
    }
}

public class DriverLocationRepository : IDriverLocationRepository
{
    private readonly AppDbContext _context;

    public DriverLocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DriverProfile?> GetDriverByUserIdAsync(int userId)
    {
        return await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
    }

    public async Task<DriverLocation?> GetByDriverProfileIdAsync(int driverProfileId)
    {
        return await _context.DriverLocations
            .FirstOrDefaultAsync(l => l.DriverProfileId == driverProfileId);
    }

    public async Task<DriverLocation> CreateAsync(DriverLocation location)
    {
        _context.DriverLocations.Add(location);
        await _context.SaveChangesAsync();
        return location;
    }

    public async Task<DriverLocation> UpdateAsync(DriverLocation location)
    {
        _context.DriverLocations.Update(location);
        await _context.SaveChangesAsync();
        return location;
    }

    public async Task<DriverProfile> UpdateDriverAsync(DriverProfile driver)
    {
        _context.DriverProfiles.Update(driver);
        await _context.SaveChangesAsync();
        return driver;
    }
}

public class FavoriteLocationRepository : IFavoriteLocationRepository
{
    private readonly AppDbContext _context;

    public FavoriteLocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FavoriteLocation>> GetByUserIdAsync(int userId)
    {
        return await _context.FavoriteLocations
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Label)
            .ToListAsync();
    }

    public async Task<FavoriteLocation?> GetByIdAsync(int id)
    {
        return await _context.FavoriteLocations.FindAsync(id);
    }

    public async Task<FavoriteLocation> CreateAsync(FavoriteLocation favorite)
    {
        _context.FavoriteLocations.Add(favorite);
        await _context.SaveChangesAsync();
        return favorite;
    }

    public async Task DeleteAsync(FavoriteLocation favorite)
    {
        _context.FavoriteLocations.Remove(favorite);
        await _context.SaveChangesAsync();
    }
}

public class ChatRepository : IChatRepository
{
    private readonly AppDbContext _context;

    public ChatRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(int rideId, int lastId)
    {
        return await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.RideId == rideId && m.Id > lastId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }
}

public class SosRepository : ISosRepository
{
    private readonly AppDbContext _context;

    public SosRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SosAlert> CreateAsync(SosAlert alert)
    {
        _context.SosAlerts.Add(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    public async Task<IEnumerable<SosAlert>> GetAllAlertsAsync()
    {
        return await _context.SosAlerts
            .Include(s => s.User)
            .Include(s => s.Ride)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<SosAlert?> GetByIdAsync(int id)
    {
        return await _context.SosAlerts.FindAsync(id);
    }

    public async Task<SosAlert> UpdateAsync(SosAlert alert)
    {
        _context.SosAlerts.Update(alert);
        await _context.SaveChangesAsync();
        return alert;
    }
}

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> CountUsersAsync() => _context.Users.CountAsync();
    public Task<int> CountDriversAsync() => _context.DriverProfiles.CountAsync();
    public Task<int> CountRidesAsync() => _context.Rides.CountAsync();
    public Task<int> CountCompletedRidesAsync() => _context.Rides.CountAsync(r => r.Status == RideStatus.Completed);

    public async Task<IEnumerable<DriverProfile>> GetPendingDriversAsync()
    {
        return await _context.DriverProfiles
            .Where(d => !d.IsApproved)
            .ToListAsync();
    }
}

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _context;

    public AnalyticsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ride>> GetAllRidesWithPaymentsAsync()
    {
        return await _context.Rides
            .Include(r => r.Payment)
            .ToListAsync();
    }

    public Task<int> CountActiveDriversAsync()
    {
        return _context.DriverProfiles.CountAsync(d => d.IsAvailable && d.IsApproved);
    }
}

public class EarningsRepository : IEarningsRepository
{
    private readonly AppDbContext _context;

    public EarningsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DriverProfile?> GetDriverByUserIdAsync(int userId)
    {
        return await _context.DriverProfiles.FirstOrDefaultAsync(d => d.UserId == userId);
    }

    public async Task<IEnumerable<Ride>> GetCompletedRidesByDriverAsync(int driverProfileId)
    {
        return await _context.Rides
            .Include(r => r.Payment)
            .Where(r => r.DriverProfileId == driverProfileId && r.Status == RideStatus.Completed)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();
    }
}

public class ReceiptRepository : IReceiptRepository
{
    private readonly AppDbContext _context;

    public ReceiptRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Ride?> GetReceiptRideAsync(int rideId)
    {
        return await _context.Rides
            .Include(r => r.Passenger)
            .Include(r => r.DriverProfile).ThenInclude(d => d!.User)
            .Include(r => r.DriverProfile).ThenInclude(d => d!.Vehicle)
            .Include(r => r.Rating)
            .Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == rideId);
    }
}