using System.Text.Json.Serialization;

namespace FeedbackApi;

public class Feedback
{
    public string id { get; set; } = $"{Guid.NewGuid()}";

    /// <summary>
    /// Score: 1 .. 5
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Details
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    public DateTime Created { get; set; }
}