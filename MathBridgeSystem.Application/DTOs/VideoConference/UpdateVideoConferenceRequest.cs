using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.VideoConference;

public class UpdateVideoConferenceRequest
{
    public DateTime? ScheduledStartTime { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    
    [MaxLength(255)]
    public string? DisplayName { get; set; }
    
    [RegularExpression("^(Scheduled|InProgress|Completed|Cancelled|Failed)$")]
    public string? Status { get; set; }
}

public class StartVideoConferenceRequest
{
    [Required]
    public Guid ConferenceId { get; set; }
}

public class EndVideoConferenceRequest
{
    [Required]
    public Guid ConferenceId { get; set; }
}

public class JoinVideoConferenceRequest
{
    [Required]
    public Guid ConferenceId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
}