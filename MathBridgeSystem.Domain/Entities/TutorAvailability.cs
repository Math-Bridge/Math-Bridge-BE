using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class TutorAvailability
{
    public Guid AvailabilityId { get; set; }

    public Guid TutorId { get; set; }

    public string TimeSlot { get; set; } = null!;

    public bool IsRecurring { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }

    public bool IsBooked { get; set; }

    public bool IsOnline { get; set; }

    public string? VideoCallPlatform { get; set; }

    public string? VideoCallLink { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual User Tutor { get; set; } = null!;
}
