using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class Curriculum
{
    public Guid CurriculumId { get; set; }

    public string CurriculumCode { get; set; } = null!;

    public string CurriculumName { get; set; } = null!;

    public string Grades { get; set; } = null!;

    public string? SyllabusUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public byte? TotalCredits { get; set; }

    public virtual ICollection<PaymentPackage> PaymentPackages { get; set; } = new List<PaymentPackage>();

    public virtual ICollection<School> Schools { get; set; } = new List<School>();

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();
}
