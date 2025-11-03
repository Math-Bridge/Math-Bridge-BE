using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class NotificationPreference
{
    public Guid PreferenceId { get; set; }

    public Guid UserId { get; set; }

    public bool EnableSessionReminders { get; set; } = true;

    public bool EnablePaymentNotifications { get; set; } = true;

    public bool EnableBookingNotifications { get; set; } = true;

    public bool EnableEmailNotifications { get; set; } = true;

    public bool EnableWebNotifications { get; set; } = true;

    public bool EnableInAppNotifications { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual User User { get; set; } = null!;
}