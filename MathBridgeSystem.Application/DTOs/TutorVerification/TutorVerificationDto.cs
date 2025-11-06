namespace MathBridgeSystem.Application.DTOs.TutorVerification
{
    public class TutorVerificationDto
    {
        public Guid VerificationId { get; set; }

        public Guid UserId { get; set; }

        public string? UserFullName { get; set; }

        public string? UserEmail { get; set; }

        public string University { get; set; } = null!;

        public string Major { get; set; } = null!;

        public decimal HourlyRate { get; set; }

        public string? Bio { get; set; }

        public string VerificationStatus { get; set; } = null!;

        public DateTime? VerificationDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsDeleted { get; set; }
    }
}
