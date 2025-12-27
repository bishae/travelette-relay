namespace Travelette.Relay.Domain.Entities;

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
            if (string.IsNullOrEmpty(Title))
                return string.Empty;
            
            // Handle old format (plain string) - migrate on the fly
            var trimmed = Title.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                // Old format: plain string, treat as English
                return Title;
            }
            
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Title) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            // If deserialization fails, treat as old format plain string
            return string.IsNullOrEmpty(Title) ? string.Empty : Title;
        }
    }

    public void SetTitle(string language, string value)
    {
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Title) ?? new Dictionary<string, string>();
            dict[language] = value;
            Title = System.Text.Json.JsonSerializer.Serialize(dict);
        }
        catch
        {
            Title = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public string GetLocation(string language = "en")
    {
        try
        {
            if (string.IsNullOrEmpty(Location))
                return string.Empty;
            
            // Handle old format (plain string) - migrate on the fly
            var trimmed = Location.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                // Old format: plain string, treat as English
                return Location;
            }
            
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Location) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            // If deserialization fails, treat as old format plain string
            return string.IsNullOrEmpty(Location) ? string.Empty : Location;
        }
    }

    public void SetLocation(string language, string value)
    {
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Location) ?? new Dictionary<string, string>();
            dict[language] = value;
            Location = System.Text.Json.JsonSerializer.Serialize(dict);
        }
        catch
        {
            Location = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public string GetDescription(string language = "en")
    {
        try
        {
            if (string.IsNullOrEmpty(Description))
                return string.Empty;
            
            // Handle old format (plain string) - migrate on the fly
            var trimmed = Description.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                // Old format: plain string, treat as English
                return Description;
            }
            
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Description) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            // If deserialization fails, treat as old format plain string
            return string.IsNullOrEmpty(Description) ? string.Empty : Description;
        }
    }

    public void SetDescription(string language, string value)
    {
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Description) ?? new Dictionary<string, string>();
            dict[language] = value;
            Description = System.Text.Json.JsonSerializer.Serialize(dict);
        }
        catch
        {
            Description = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }

    public List<string> GetInclusions(string language = "en")
    {
        try
        {
            // Handle old format - check if it's a JSON array of dictionaries
            if (string.IsNullOrEmpty(Inclusions) || !Inclusions.TrimStart().StartsWith("["))
            {
                // Old format: might be stored differently, return empty for now
                // Could be a delimited string in old format, but we'll handle it as empty
                return new List<string>();
            }
            
            var list = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Inclusions) ?? new List<Dictionary<string, string>>();
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
            var existing = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Inclusions) ?? new List<Dictionary<string, string>>();
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
            Inclusions = System.Text.Json.JsonSerializer.Serialize(existing);
        }
        catch
        {
            var list = values.Select(v => new Dictionary<string, string> { { language, v } }).ToList();
            Inclusions = System.Text.Json.JsonSerializer.Serialize(list);
        }
    }

    public List<string> GetExclusions(string language = "en")
    {
        try
        {
            // Handle old format - check if it's a JSON array of dictionaries
            if (string.IsNullOrEmpty(Exclusions) || !Exclusions.TrimStart().StartsWith("["))
            {
                // Old format: might be stored differently, return empty for now
                // Could be a delimited string in old format, but we'll handle it as empty
                return new List<string>();
            }
            
            var list = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Exclusions) ?? new List<Dictionary<string, string>>();
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
            var existing = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(Exclusions) ?? new List<Dictionary<string, string>>();
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
            Exclusions = System.Text.Json.JsonSerializer.Serialize(existing);
        }
        catch
        {
            var list = values.Select(v => new Dictionary<string, string> { { language, v } }).ToList();
            Exclusions = System.Text.Json.JsonSerializer.Serialize(list);
        }
    }
}

