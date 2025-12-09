using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class RescheduleRequest
{
    public Guid RequestId { get; set; }

    public Guid BookingId { get; set; }

    public Guid ParentId { get; set; }

    public DateOnly RequestedDate { get; set; }

    public Guid? RequestedTutorId { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public Guid? StaffId { get; set; }

    public DateTime? ProcessedDate { get; set; }

    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// FK to contracts. One contract has many reschedule requests (1-N).
    /// </summary>
    public Guid ContractId { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual Session Booking { get; set; } = null!;

    public virtual Contract Contract { get; set; } = null!;

    public virtual User Parent { get; set; } = null!;

    public virtual User? RequestedTutor { get; set; }

    public virtual User? Staff { get; set; }
}
