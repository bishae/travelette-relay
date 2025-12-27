namespace Travelette.Relay.Domain.Entities;

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

    public string GetActivity(string language = "en")
    {
        try
        {
            if (string.IsNullOrEmpty(Activity))
                return string.Empty;
            
            // Handle old format (plain string) - migrate on the fly
            var trimmed = Activity.TrimStart();
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
            {
                // Old format: plain string, treat as English
                return Activity;
            }
            
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Activity) ?? new Dictionary<string, string>();
            return dict.TryGetValue(language, out var value) ? value : (dict.TryGetValue("en", out var enValue) ? enValue : string.Empty);
        }
        catch
        {
            // If deserialization fails, treat as old format plain string
            return string.IsNullOrEmpty(Activity) ? string.Empty : Activity;
        }
    }

    public void SetActivity(string language, string value)
    {
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(Activity) ?? new Dictionary<string, string>();
            dict[language] = value;
            Activity = System.Text.Json.JsonSerializer.Serialize(dict);
        }
        catch
        {
            Activity = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { { language, value } });
        }
    }
}

