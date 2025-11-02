using System;

namespace MathBridgeSystem.Application.DTOs.Curriculum
{
    public class CurriculumDto
    {
        public Guid CurriculumId { get; set; }
        public string CurriculumCode { get; set; } = string.Empty;
        public string CurriculumName { get; set; } = string.Empty;
        public string Grades { get; set; } = string.Empty;
        public string? SyllabusUrl { get; set; }
        public bool IsActive { get; set; }
        public byte? TotalCredits { get; set; }
        public int TotalSchools { get; set; }
        public int TotalPackages { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}