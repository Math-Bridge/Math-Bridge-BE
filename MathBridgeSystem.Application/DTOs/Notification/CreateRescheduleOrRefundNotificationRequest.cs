using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Notification;

public class CreateRescheduleOrRefundNotificationRequest
{
    [Required]
    public Guid RequestId { get; set; }
    [Required]
    public Guid ContractId { get; set; }

    [Required]
    public Guid BookingId { get; set; }
}
