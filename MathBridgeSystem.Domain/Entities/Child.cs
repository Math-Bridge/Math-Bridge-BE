using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class Child
{
    public Guid ChildId { get; set; }

    public Guid ParentId { get; set; }

    public string FullName { get; set; } = null!;

    public string Grade { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid? CenterId { get; set; }

    public string Status { get; set; } = null!;

    public Guid SchoolId { get; set; }

    public string? CurrentTopic { get; set; }

    public DateTime? LastTopicUpdate { get; set; }

    public virtual Center? Center { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual User Parent { get; set; } = null!;

    public virtual School School { get; set; } = null!;

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
