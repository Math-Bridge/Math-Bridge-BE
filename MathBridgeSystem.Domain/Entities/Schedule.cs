using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class Schedule
{
    public Guid BookingId { get; set; }

    public Guid ContractId { get; set; }

    public Guid TutorId { get; set; }

    public DateOnly SessionDate { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsOnline { get; set; }

    public string? VideoCallPlatform { get; set; }

    public string? VideoCallLink { get; set; }

    public string? OfflineAddress { get; set; }

    public decimal? OfflineLatitude { get; set; }

    public decimal? OfflineLongitude { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<RescheduleRequest> RescheduleRequests { get; set; } = new List<RescheduleRequest>();

    public virtual User Tutor { get; set; } = null!;
}
