using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.School
{
    public class CreateSchoolRequest
    {
        [Required]
        [StringLength(200)]
        public string SchoolName { get; set; } = string.Empty;

        [Required]
        public Guid CurriculumId { get; set; }
    }
}