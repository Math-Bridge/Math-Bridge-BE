using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class MathConcept
{
    public Guid ConceptId { get; set; }

    public string? Name { get; set; }

    public string? Category { get; set; }

    public Guid? UnitId { get; set; }

    public virtual Unit? Unit { get; set; }
}
