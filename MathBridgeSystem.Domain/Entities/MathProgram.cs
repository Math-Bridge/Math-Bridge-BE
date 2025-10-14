using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class MathProgram
{
    public Guid ProgramId { get; set; }

    public string ProgramName { get; set; } = null!;

    public string? Description { get; set; }

    public string? LinkSyllabus { get; set; }

    public virtual ICollection<PaymentPackage> PaymentPackages { get; set; } = new List<PaymentPackage>();

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
