using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces;

public interface IVideoConferenceProvider
{
    string PlatformName { get; }

    Task<VideoConferenceCreationResult> CreateMeetingAsync();
    
    
}

public class VideoConferenceCreationResult
{
    public string MeetingId { get; set; } = null!;
    public string MeetingUri { get; set; } = null!;
    public string? MeetingCode { get; set; }
    public string? SpaceName { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MeetingDetailsResult
{
    public string MeetingId { get; set; } = null!;
    public string MeetingUri { get; set; } = null!;
    public string? MeetingCode { get; set; }
    public string? Status { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}