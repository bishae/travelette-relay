using System.Text.Json;

namespace Travelette.Relay.DTOs;

public class TripDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty; // Translated based on language parameter
    public string Location { get; set; } = string.Empty; // Translated based on language parameter
    public string Date { get; set; } = string.Empty; // Formatted date string for display
    public string Duration { get; set; } = string.Empty; // Formatted duration string for display
    public DateTime StartDate { get; set; } // Raw date for editing
    public int DurationDays { get; set; } // Raw duration for editing
    public string Price { get; set; } = string.Empty; // Formatted price string for display
    public decimal PriceAmount { get; set; } // Raw price for editing
    public string Description { get; set; } = string.Empty; // Translated based on language parameter
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<ItineraryDayDto> Itinerary { get; set; } = new();
    public List<string> Inclusions { get; set; } = new(); // Translated based on language parameter
    public List<string> Exclusions { get; set; } = new(); // Translated based on language parameter
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ItineraryDayDto
{
    public Guid Id { get; set; }
    public int Day { get; set; }
    public string Title { get; set; } = string.Empty; // Translated based on language parameter
    public string Activity { get; set; } = string.Empty; // Translated based on language parameter
}

// DTOs for creating/updating trips - accept dictionaries for multi-language support
public class CreateTripDto
{
    public Dictionary<string, string> Title { get; set; } = new(); // { "en": "...", "ar": "..." }
    public Dictionary<string, string> Location { get; set; } = new();
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public Dictionary<string, string> Description { get; set; } = new();
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<CreateItineraryDayDto> Itinerary { get; set; } = new();
    public List<Dictionary<string, string>> Inclusions { get; set; } = new(); // Array of { "en": "...", "ar": "..." }
    public List<Dictionary<string, string>> Exclusions { get; set; } = new();
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
}

public class CreateItineraryDayDto
{
    public int Day { get; set; }
    public Dictionary<string, string> Title { get; set; } = new(); // { "en": "...", "ar": "..." }
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

