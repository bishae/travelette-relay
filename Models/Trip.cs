using System.Text.Json;

namespace Travelette.Relay.Models;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    // Multi-language fields stored as JSON: { "en": "English text", "ar": "Arabic text" }
    public string Title { get; set; } = "{}"; // JSON object with language keys
    public string Location { get; set; } = "{}"; // JSON object with language keys
    public DateTime StartDate { get; set; }
    public int DurationDays { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = "{}"; // JSON object with language keys
    public string HeroImage { get; set; } = string.Empty;
    public List<string> Gallery { get; set; } = new();
    public List<ItineraryDay> Itinerary { get; set; } = new();
    public string Inclusions { get; set; } = "[]"; // JSON array of objects with language keys
    public string Exclusions { get; set; } = "[]"; // JSON array of objects with language keys
    public int SpotsTotal { get; set; }
    public int SpotsLeft { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Helper methods to get/set translated values
    public string GetTitle(string language = "en")
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Title) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetTitle(string language, string value)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Title) ?? new Dictionary<string, string>();
            dict[language] = value;
            Title = JsonSerializer.Serialize(dict);
        }
        catch
        {
            Title = JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public string GetLocation(string language = "en")
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Location) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetLocation(string language, string value)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Location) ?? new Dictionary<string, string>();
            dict[language] = value;
            Location = JsonSerializer.Serialize(dict);
        }
        catch
        {
            Location = JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public string GetDescription(string language = "en")
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Description) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetDescription(string language, string value)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Description) ?? new Dictionary<string, string>();
            dict[language] = value;
            Description = JsonSerializer.Serialize(dict);
        }
        catch
        {
            Description = JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public List<string> GetInclusions(string language = "en")
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Inclusions) ?? new List<Dictionary<string, string>>();
            return list.Select(item => item.TryGetValue(language, out var value) ? value : (item.TryGetValue("en", out var enValue) ? enValue : string.Empty)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetInclusions(string language, List<string> values)
    {
        try
        {
            var existing = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Inclusions) ?? new List<Dictionary<string, string>>();
            // Update or add items
            for (int i = 0; i < values.Count; i++)
            {
                if (i < existing.Count)
                {
                    existing[i][language] = values[i];
                }
                else
                {
                    existing.Add(new Dictionary<string, string> { { language, values[i] } });
                }
            }
            Inclusions = JsonSerializer.Serialize(existing);
        }
        catch
        {
            var list = values.Select(v => new Dictionary<string, string> { { language, v } }).ToList();
            Inclusions = JsonSerializer.Serialize(list);
        }
    }

    public List<string> GetExclusions(string language = "en")
    {
        try
        {
            var list = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Exclusions) ?? new List<Dictionary<string, string>>();
            return list.Select(item => item.TryGetValue(language, out var value) ? value : (item.TryGetValue("en", out var enValue) ? enValue : string.Empty)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetExclusions(string language, List<string> values)
    {
        try
        {
            var existing = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Exclusions) ?? new List<Dictionary<string, string>>();
            // Update or add items
            for (int i = 0; i < values.Count; i++)
            {
                if (i < existing.Count)
                {
                    existing[i][language] = values[i];
                }
                else
                {
                    existing.Add(new Dictionary<string, string> { { language, values[i] } });
                }
            }
            Exclusions = JsonSerializer.Serialize(existing);
        }
        catch
        {
            var list = values.Select(v => new Dictionary<string, string> { { language, v } }).ToList();
            Exclusions = JsonSerializer.Serialize(list);
        }
    }
}

public class ItineraryDay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }
    public int Day { get; set; }
    // Multi-language fields stored as JSON: { "en": "English text", "ar": "Arabic text" }
    public string Title { get; set; } = "{}"; // JSON object with language keys
    public string Activity { get; set; } = "{}"; // JSON object with language keys

    // Helper methods to get/set translated values
    public string GetTitle(string language = "en")
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Title) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetTitle(string language, string value)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Title) ?? new Dictionary<string, string>();
            dict[language] = value;
            Title = JsonSerializer.Serialize(dict);
        }
        catch
        {
            Title = JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public string GetActivity(string language = "en")
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Activity) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetActivity(string language, string value)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(Activity) ?? new Dictionary<string, string>();
            dict[language] = value;
            Activity = JsonSerializer.Serialize(dict);
        }
        catch
        {
            Activity = JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }
}

