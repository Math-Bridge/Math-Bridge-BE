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