using Microsoft.EntityFrameworkCore;
using Travelette.Relay.Models;

namespace Travelette.Relay.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Trip> Trips { get; set; }
    public DbSet<ItineraryDay> ItineraryDays { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Gallery)
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.Inclusions)
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList());
            entity.Property(e => e.Exclusions)
                .HasConversion(
                    v => string.Join("|||", v),
                    v => v.Split("|||", StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<ItineraryDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Trip)
                .WithMany(t => t.Itinerary)
                .HasForeignKey(e => e.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

