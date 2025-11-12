using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class PaymentPackage
{
    public Guid PackageId { get; set; }

    public string PackageName { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public decimal Price { get; set; }

    public int SessionCount { get; set; }

    public int SessionsPerWeek { get; set; }

    public int MaxReschedule { get; set; }

    public int DurationDays { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid CurriculumId { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual Curriculum Curriculum { get; set; } = null!;
}
