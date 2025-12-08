using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class ContractSchedule
{
    public Guid ScheduleId { get; set; }

    public Guid ContractId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Contract Contract { get; set; } = null!;
}
