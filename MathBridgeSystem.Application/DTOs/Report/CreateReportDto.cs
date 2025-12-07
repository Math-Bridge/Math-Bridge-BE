using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Report
{
    /// <summary>
    /// DTO for creating a report. Type is automatically set based on the creator's role:
    /// - RoleId 2 (tutor) ? Type = "tutor"
    /// - RoleId 3 (parent) ? Type = "parent"
    /// </summary>
    public class CreateReportDto
    {
        /// <summary>
        /// The ID of the tutor being reported (required for parent reports)
        /// or the parent being reported to (required for tutor reports)
        /// </summary>
        public Guid? TutorId { get; set; }

        /// <summary>
        /// The ID of the parent (required for tutor reports)
        /// </summary>
        public Guid? ParentId { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Content must be at least 10 characters.")]
        [MaxLength(2000, ErrorMessage = "Content cannot exceed 2000 characters.")]
        public string Content { get; set; } = null!;

        public string? Url { get; set; }

        [Required]
        public Guid ContractId { get; set; }
    }
}