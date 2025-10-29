using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class TutorSchedule
{
    public Guid AvailabilityId { get; set; }

    public Guid TutorId { get; set; }

    public byte DaysOfWeek { get; set; }

    public TimeOnly AvailableFrom { get; set; }

    public TimeOnly AvailableUntil { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveUntil { get; set; }

    public bool CanTeachOnline { get; set; }

    public bool CanTeachOffline { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool? IsBooked { get; set; }

    public virtual User Tutor { get; set; } = null!;
}
