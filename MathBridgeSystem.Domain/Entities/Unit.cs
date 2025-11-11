using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class Unit
{
    public Guid UnitId { get; set; }

    public Guid CurriculumId { get; set; }

    public string UnitName { get; set; } = null!;

    public string? UnitDescription { get; set; }

    public int UnitOrder { get; set; }

    public byte? Credit { get; set; }

    public string? LearningObjectives { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Curriculum Curriculum { get; set; } = null!;

    public virtual ICollection<DailyReport> DailyReports { get; set; } = new List<DailyReport>();

    public virtual User? UpdatedByNavigation { get; set; }
}
