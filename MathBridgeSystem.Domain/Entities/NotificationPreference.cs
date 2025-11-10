using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class NotificationPreference
{
    public Guid PreferenceId { get; set; }

    public Guid UserId { get; set; }

    public bool ReceiveEmailNotifications { get; set; }

    public bool ReceiveSmsnotifications { get; set; }

    public bool ReceiveWebNotifications { get; set; }

    public bool ReceiveSessionReminders { get; set; }

    public bool ReceiveContractUpdates { get; set; }

    public bool ReceivePaymentNotifications { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual User User { get; set; } = null!;
}
