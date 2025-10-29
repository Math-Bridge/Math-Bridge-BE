using System;

namespace MathBridgeSystem.Application.DTOs.VideoConference;

public class VideoConferenceSessionDto
{
    public Guid ConferenceId { get; set; }
    public Guid BookingId { get; set; }
    public Guid ContractId { get; set; }
    public string Platform { get; set; } = null!;
    public string? SpaceName { get; set; }
    public string? SpaceId { get; set; }
    public string? MeetingUri { get; set; }
    public string? MeetingCode { get; set; }
    public string? DisplayName { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public DateTime? ActualStartTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public string? Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<VideoConferenceParticipantDto>? Participants { get; set; }
}

public class VideoConferenceParticipantDto
{
    public Guid ParticipantId { get; set; }
    public Guid ConferenceId { get; set; }
    public Guid UserId { get; set; }
    public string ParticipantType { get; set; } = null!;
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Status { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}