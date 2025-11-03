using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class NotificationLog
{
    public Guid LogId { get; set; }

    public Guid NotificationId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? SessionId { get; set; }

    public string Channel { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? FailureReason { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? SentDate { get; set; }

    public DateTime? DeliveredDate { get; set; }

    public virtual Notification Notification { get; set; } = null!;

    public virtual Contract? Contract { get; set; }

    public virtual Session? Session { get; set; }
}