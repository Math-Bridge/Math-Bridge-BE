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
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}