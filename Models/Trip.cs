namespace Travelette.Relay.Models;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<ItineraryDay> Itinerary { get; set; } = new();
    public List<string> Inclusions { get; set; } = new();
    public List<string> Exclusions { get; set; } = new();
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ItineraryDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }
    public int Day { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
}

