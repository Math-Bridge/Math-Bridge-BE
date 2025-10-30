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
    [RegularExpression("^(Meet|Zoom)$", ErrorMessage = "Platform must be either 'Meet' or 'Zoom'")]
    public string Platform { get; set; } = null!;
    
    
}