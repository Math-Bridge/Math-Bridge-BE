using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class Notification
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? BookingId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string NotificationType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? SentDate { get; set; }

    public virtual Schedule? Booking { get; set; }

    public virtual Contract? Contract { get; set; }

    public virtual User User { get; set; } = null!;
}
