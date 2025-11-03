namespace MathBridgeSystem.Application.DTOs.VideoConference;

public class ZoomMeetingResponse
{
    public long Id { get; set; }
    public string? Topic { get; set; }
    public string? JoinUrl { get; set; }
    public string? StartUrl { get; set; }
    public string? Status { get; set; }
    public DateTime? StartTime { get; set; }
    public int Duration { get; set; }
}