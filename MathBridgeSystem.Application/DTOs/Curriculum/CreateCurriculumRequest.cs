using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Curriculum
{
    public class CreateCurriculumRequest
    {
        [Required]
        [StringLength(20)]
        public string CurriculumCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CurriculumName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Grades { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SyllabusUrl { get; set; }

        public byte? TotalCredits { get; set; }
    }
}