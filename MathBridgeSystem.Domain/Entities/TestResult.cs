using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class TestResult
{
    public Guid ResultId { get; set; }

    public Guid TutorId { get; set; }

    public Guid ChildId { get; set; }

    public Guid MathProgramId { get; set; }

    public string TestName { get; set; } = null!;

    public string TestType { get; set; } = null!;

    public decimal Score { get; set; }

    public decimal MaxScore { get; set; }

    public decimal? Percentage { get; set; }

    public int? DurationMinutes { get; set; }

    public int? NumberOfQuestions { get; set; }

    public int? CorrectAnswers { get; set; }

    public string? Notes { get; set; }

    public string? AreasForImprovement { get; set; }

    public DateTime TestDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public virtual Child Child { get; set; } = null!;

    public virtual MathProgram MathProgram { get; set; } = null!;

    public virtual User Tutor { get; set; } = null!;
}
