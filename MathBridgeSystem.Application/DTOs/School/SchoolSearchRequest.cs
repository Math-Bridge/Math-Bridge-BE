using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.School
{
    public class SchoolSearchRequest
    {
        public string? Name { get; set; }

        public Guid? CurriculumId { get; set; }

        public bool? IsActive { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;
    }
}