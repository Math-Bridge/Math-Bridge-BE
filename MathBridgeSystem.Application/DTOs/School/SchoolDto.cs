using System;

namespace MathBridgeSystem.Application.DTOs.School
{
    public class SchoolDto
    {
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public Guid CurriculumId { get; set; }
        public string? CurriculumName { get; set; }
        public bool IsActive { get; set; }
        public int TotalChildren { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}