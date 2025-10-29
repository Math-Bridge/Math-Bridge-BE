using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.VideoConference;

public class CreateVideoConferenceRequest
{
    [Required]
    public Guid BookingId { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    [RegularExpression("^(GoogleMeet|Zoom)$", ErrorMessage = "Platform must be either 'GoogleMeet' or 'Zoom'")]
    public string Platform { get; set; } = null!;

    [Required]
    public DateTime ScheduledStartTime { get; set; }

    [Required]
    public DateTime ScheduledEndTime { get; set; }

    [MaxLength(255)]
    public string? DisplayName { get; set; }

    public List<Guid>? ParticipantUserIds { get; set; }
}