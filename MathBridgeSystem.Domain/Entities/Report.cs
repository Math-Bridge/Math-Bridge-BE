using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class Report
{
    public Guid ReportId { get; set; }

    public Guid ParentId { get; set; }

    public Guid TutorId { get; set; }

    public string Content { get; set; } = null!;

    public string? Url { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly CreatedDate { get; set; }

    public string? Type { get; set; }

    public Guid ContractId { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual User Parent { get; set; } = null!;

    public virtual User Tutor { get; set; } = null!;
}
