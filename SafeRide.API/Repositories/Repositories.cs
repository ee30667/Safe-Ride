using Microsoft.EntityFrameworkCore;
using SafeRide.API.Data;
using SafeRide.API.Interfaces;
using SafeRide.API.Models;

namespace SafeRide.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(int id) =>
        await _context.Users.Include(u => u.DriverProfile).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users.Include(u => u.DriverProfile).ToListAsync();

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
        if (user != null) { _context.Users.Remove(user); await _context.SaveChangesAsync(); }
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _context.Users.AnyAsync(u => u.Id == id);
}

public class DriverRepository : IDriverRepository
{
    private readonly AppDbContext _context;
    public DriverRepository(AppDbContext context) => _context = context;

    public async Task<DriverProfile?> GetByIdAsync(int id) =>
        await _context.DriverProfiles.Include(d => d.User).Include(d => d.Vehicle).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<DriverProfile?> GetByUserIdAsync(int userId) =>
        await _context.DriverProfiles.Include(d => d.User).Include(d => d.Vehicle).FirstOrDefaultAsync(d => d.UserId == userId);

    public async Task<IEnumerable<DriverProfile>> GetAllAsync() =>
        await _context.DriverProfiles.Include(d => d.User).Include(d => d.Vehicle).ToListAsync();

    public async Task<IEnumerable<DriverProfile>> GetAvailableDriversAsync() =>
        await _context.DriverProfiles.Include(d => d.User).Include(d => d.Vehicle)
            .Where(d => d.IsAvailable && d.IsApproved && d.User.IsActive).ToListAsync();

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
    public RideRepository(AppDbContext context) => _context = context;

    private IQueryable<Ride> FullQuery() =>
        _context.Rides.Include(r => r.Passenger).Include(r => r.DriverProfile).ThenInclude(d => d!.User).Include(r => r.Rating);

    public async Task<Ride?> GetByIdAsync(int id) => await FullQuery().FirstOrDefaultAsync(r => r.Id == id);
    public async Task<IEnumerable<Ride>> GetAllAsync() => await FullQuery().OrderByDescending(r => r.RequestedAt).ToListAsync();
    public async Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId) =>
        await FullQuery().Where(r => r.PassengerId == passengerId).OrderByDescending(r => r.RequestedAt).ToListAsync();
    public async Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId) =>
        await FullQuery().Where(r => r.DriverProfileId == driverProfileId).OrderByDescending(r => r.RequestedAt).ToListAsync();
    public async Task<Ride?> GetActiveRideForPassengerAsync(int passengerId) =>
        await FullQuery().FirstOrDefaultAsync(r => r.PassengerId == passengerId &&
            (r.Status == RideStatus.Requested || r.Status == RideStatus.Accepted || r.Status == RideStatus.InProgress));

    public async Task<Ride> CreateAsync(Ride ride) { _context.Rides.Add(ride); await _context.SaveChangesAsync(); return ride; }
    public async Task<Ride> UpdateAsync(Ride ride) { _context.Rides.Update(ride); await _context.SaveChangesAsync(); return ride; }
}

public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _context;
    public RatingRepository(AppDbContext context) => _context = context;

    public async Task<RideRating?> GetByRideIdAsync(int rideId) =>
        await _context.RideRatings.Include(r => r.Rater).FirstOrDefaultAsync(r => r.RideId == rideId);

    public async Task<IEnumerable<RideRating>> GetByDriverAsync(int driverProfileId) =>
        await _context.RideRatings.Include(r => r.Rater).Include(r => r.Ride)
            .Where(r => r.Ride.DriverProfileId == driverProfileId).ToListAsync();

    public async Task<RideRating> CreateAsync(RideRating rating)
    {
        _context.RideRatings.Add(rating);
        await _context.SaveChangesAsync();
        return rating;
    }
}
