using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class RescheduleRequest
{
    public Guid RequestId { get; set; }

    public Guid BookingId { get; set; }

    public Guid ParentId { get; set; }

    public string RequestedTimeSlot { get; set; } = null!;

    public DateOnly RequestedDate { get; set; }

    public Guid? RequestedTutorId { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public Guid? StaffId { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Schedule Booking { get; set; } = null!;

    public virtual User Parent { get; set; } = null!;

    public virtual User? RequestedTutor { get; set; }

    public virtual User? Staff { get; set; }
}
