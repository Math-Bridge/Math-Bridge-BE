namespace MathBridgeSystem.Application.DTOs.VideoConference;

public class VideoConferenceCreationResult
{
    public string MeetingId { get; set; } = null!;
    public string MeetingUri { get; set; } = null!;
    public string? MeetingCode { get; set; }
    public string? SpaceName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}