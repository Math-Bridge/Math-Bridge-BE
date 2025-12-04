using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class School
{
    public Guid SchoolId { get; set; }

    public string SchoolName { get; set; } 

    public Guid CurriculumId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual ICollection<Child> Children { get; set; } = new List<Child>();

    public virtual Curriculum Curriculum { get; set; } = null!;
}
