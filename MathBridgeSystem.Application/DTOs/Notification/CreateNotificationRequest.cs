using System;

namespace MathBridgeSystem.Application.DTOs.Notification;

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? BookingId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string NotificationType { get; set; } = null!;
}