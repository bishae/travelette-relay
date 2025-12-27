namespace Travelette.Relay.Application.DTOs;

public class TripDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public string Price { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<ItineraryDayDto> Itinerary { get; set; } = new();
    public List<string> Inclusions { get; set; } = new();
    public List<string> Exclusions { get; set; } = new();
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ItineraryDayDto
{
    public Guid Id { get; set; }
    public int Day { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
}

public class CreateTripDto
{
    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Location { get; set; } = new();
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public Dictionary<string, string> Description { get; set; } = new();
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<CreateItineraryDayDto> Itinerary { get; set; } = new();
    public List<Dictionary<string, string>> Inclusions { get; set; } = new();
    public List<Dictionary<string, string>> Exclusions { get; set; } = new();
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
}

public class CreateItineraryDayDto
{
    public int Day { get; set; }
    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Activity { get; set; } = new();
}

public class UpdateTripDto
{
    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Location { get; set; } = new();
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public Dictionary<string, string> Description { get; set; } = new();
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<CreateItineraryDayDto> Itinerary { get; set; } = new();
    public List<Dictionary<string, string>> Inclusions { get; set; } = new();
    public List<Dictionary<string, string>> Exclusions { get; set; } = new();
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
}

public class RefundAllBookingsDto
{
    public string? Reason { get; set; }
}

