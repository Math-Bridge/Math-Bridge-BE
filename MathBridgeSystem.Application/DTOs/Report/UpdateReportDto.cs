using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Report
{
    /// <summary>
    /// DTO for updating a report. Type cannot be modified after creation.
    /// </summary>
    public class UpdateReportDto
    {
        [MinLength(10, ErrorMessage = "Content must be at least 10 characters.")]
        [MaxLength(2000, ErrorMessage = "Content cannot exceed 2000 characters.")]
        public string? Content { get; set; }

        public string? Url { get; set; }
    }
}
