using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.School
{
    public class UpdateSchoolRequest
    {
        [StringLength(200)]
        public string? SchoolName { get; set; }

        public Guid? CurriculumId { get; set; }

        public bool? IsActive { get; set; }
    }
}