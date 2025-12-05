using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs
{
    public class UnitDto
    {
        public Guid UnitId { get; set; }
        public Guid CurriculumId { get; set; }
        public string CurriculumName { get; set; } = null!;
        public string UnitName { get; set; } = null!;
        public string? UnitDescription { get; set; }
        public int UnitOrder { get; set; }
        public decimal? Credit { get; set; }
        public string? LearningObjectives { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedByName { get; set; }
        public List<MathConceptSummaryDto>? MathConcepts { get; set; }
    }

    public class MathConceptSummaryDto
    {
        public Guid ConceptId { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
    }

    public class CreateUnitRequest
    {
        public Guid CurriculumId { get; set; }
        public string UnitName { get; set; } = null!;
        public string? UnitDescription { get; set; }
        public int? UnitOrder { get; set; }
        public byte? Credit { get; set; }
        public string? LearningObjectives { get; set; }
        public bool IsActive { get; set; } = true;
        public List<Guid>? ConceptIds { get; set; }
    }

    public class UpdateUnitRequest
    {
        public string UnitName { get; set; } = null!;
        public string? UnitDescription { get; set; }
        public int UnitOrder { get; set; }
        public byte? Credit { get; set; }
        public string? LearningObjectives { get; set; }
        public bool IsActive { get; set; }
        public List<Guid>? ConceptIds { get; set; }
    }
}