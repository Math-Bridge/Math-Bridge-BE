using System;

namespace MathBridgeSystem.Application.DTOs.Contract
{
    public class AvailableTutorResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public decimal AverageRating { get; set; }
        public int FeedbackCount { get; set; }
    }
}
