using Microsoft.EntityFrameworkCore;
using SafeRide.API.Models;

namespace SafeRide.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<RideRating> RideRatings => Set<RideRating>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SosAlert> SosAlerts => Set<SosAlert>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<FavoriteLocation> FavoriteLocations => Set<FavoriteLocation>();
    public DbSet<DriverLocation> DriverLocations => Set<DriverLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e => {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("Passenger");
        });

        modelBuilder.Entity<DriverProfile>(e => {
            e.HasOne(d => d.User).WithOne(u => u.DriverProfile)
             .HasForeignKey<DriverProfile>(d => d.UserId);
        });

        modelBuilder.Entity<Vehicle>(e => {
            e.HasOne(v => v.DriverProfile).WithOne(d => d.Vehicle)
             .HasForeignKey<Vehicle>(v => v.DriverProfileId);
        });

        modelBuilder.Entity<Ride>(e => {
            e.HasOne(r => r.Passenger).WithMany(u => u.RidesAsPassenger)
             .HasForeignKey(r => r.PassengerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.DriverProfile).WithMany(d => d.Rides)
             .HasForeignKey(r => r.DriverProfileId).OnDelete(DeleteBehavior.Restrict);
            e.Property(r => r.EstimatedFare).HasColumnType("decimal(10,2)");
            e.Property(r => r.FinalFare).HasColumnType("decimal(10,2)");
            e.Property(r => r.SurgeMultiplier).HasColumnType("decimal(4,2)");
        });

        modelBuilder.Entity<RideRating>(e => {
            e.HasOne(rr => rr.Ride).WithOne(r => r.Rating)
             .HasForeignKey<RideRating>(rr => rr.RideId);
            e.HasOne(rr => rr.Rater).WithMany(u => u.RatingsGiven)
             .HasForeignKey(rr => rr.RaterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChatMessage>(e => {
            e.HasOne(c => c.Ride).WithMany(r => r.ChatMessages).HasForeignKey(c => c.RideId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Sender).WithMany().HasForeignKey(c => c.SenderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(e => {
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SosAlert>(e => {
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Ride).WithMany().HasForeignKey(s => s.RideId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e => {
            e.HasOne(p => p.Ride).WithOne(r => r.Payment).HasForeignKey<Payment>(p => p.RideId);
            e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<FavoriteLocation>(e => {
            e.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DriverLocation>(e => {
            e.HasOne(d => d.DriverProfile).WithMany().HasForeignKey(d => d.DriverProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            FullName = "Admin",
            Email = "admin@saferide.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            IsVerified = true,
            IsActive = true,
            PhoneNumber = "0000000000",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
