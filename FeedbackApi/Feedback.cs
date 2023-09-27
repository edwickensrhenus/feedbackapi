namespace FeedbackApi;

public class Feedback
{
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
}