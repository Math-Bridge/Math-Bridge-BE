using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Curriculum
{
    public class UpdateCurriculumRequest
    {
        [StringLength(20)]
        public string? CurriculumCode { get; set; }

        [StringLength(100)]
        public string? CurriculumName { get; set; }

        [StringLength(20)]
        public string? Grades { get; set; }

        [StringLength(500)]
        public string? SyllabusUrl { get; set; }

        public byte? TotalCredits { get; set; }

        public bool? IsActive { get; set; }
    }
}