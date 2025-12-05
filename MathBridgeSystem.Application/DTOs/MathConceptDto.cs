using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs
{
    public class MathConceptDto
    {
        public Guid ConceptId { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public List<UnitSummaryDto>? LinkedUnits { get; set; }
    }

    public class UnitSummaryDto
    {
        public Guid UnitId { get; set; }
        public string UnitName { get; set; } = null!;
        public string? CurriculumName { get; set; }
        public int UnitOrder { get; set; }
    }

    public class CreateMathConceptRequest
    {
        public string Name { get; set; } = null!;
        public string? Category { get; set; }
        public List<Guid>? UnitIds { get; set; }
    }

    public class UpdateMathConceptRequest
    {
        public string Name { get; set; } = null!;
        public string? Category { get; set; }
    }

    public class LinkMathConceptToUnitsRequest
    {
        public List<Guid> UnitIds { get; set; } = new List<Guid>();
    }

    public class UnlinkMathConceptFromUnitsRequest
    {
        public List<Guid> UnitIds { get; set; } = new List<Guid>();
    }
}
