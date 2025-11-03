using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class CreateRescheduleRequestDto
    {
        public Guid BookingId { get; set; }
        public DateOnly RequestedDate { get; set; }
        public string RequestedTimeSlot { get; set; } = null!;
        public Guid? RequestedTutorId { get; set; }
        public string? Reason { get; set; }
    }
}