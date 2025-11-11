using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class DailyReport
{
    public Guid? ChildId { get; set; }

    public Guid? TutorId { get; set; }

    public Guid? BookingId { get; set; }

    public string? Notes { get; set; }

    public Guid ReportId { get; set; }

    public bool? OnTrack { get; set; }

    public bool? HaveHomework { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public Guid? UnitId { get; set; }

    public virtual Session? Booking { get; set; }

    public virtual Child? Child { get; set; }

    public virtual User? Tutor { get; set; }

    public virtual Unit? Unit { get; set; }
}
