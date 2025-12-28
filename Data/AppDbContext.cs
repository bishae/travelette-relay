// Legacy compatibility file - kept for migrations compatibility
// All new code should use Travelette.Relay.Infrastructure.Data.AppDbContext
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Travelette.Relay.Domain.Entities;

namespace Travelette.Relay.Data;

// This is kept for backwards compatibility with existing migrations
// which reference "Travelette.Relay.Data.AppDbContext"
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Trip> Trips { get; set; }
    public DbSet<ItineraryDay> ItineraryDays { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Gallery)
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList(),
                    new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToList()));
        });

        modelBuilder.Entity<ItineraryDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Itinerary)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Trip)
                .WithMany()
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status)
                .HasConversion<string>();
        });
    }
}
