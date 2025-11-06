using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.TutorVerification
{
    public class UpdateTutorVerificationRequest
    {
        [StringLength(255, MinimumLength = 2, ErrorMessage = "University must be between 2 and 255 characters")]
        public string? University { get; set; }

        [StringLength(255, MinimumLength = 2, ErrorMessage = "Major must be between 2 and 255 characters")]
        public string? Major { get; set; }

        [Range(0.01, 1000000, ErrorMessage = "Hourly rate must be between 0.01 and 1000000")]
        public decimal? HourlyRate { get; set; }

        [StringLength(5000, ErrorMessage = "Bio cannot exceed 5000 characters")]
        public string? Bio { get; set; }
    }
}
