using System;

namespace MathBridgeSystem.Application.DTOs.TutorSchedule
{
    public class TutorScheduleResponse
    {
        public Guid AvailabilityId { get; set; }
        public Guid TutorId { get; set; }
        public string TutorName { get; set; }
        public int DayOfWeek { get; set; }
        public string DayOfWeekName { get; set; }
        public TimeOnly AvailableFrom { get; set; }
        public TimeOnly AvailableUntil { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveUntil { get; set; }
        public int MaxConcurrentBookings { get; set; }
        public int CurrentBookings { get; set; }
        public int AvailableSlots { get; set; }
        public bool CanTeachOnline { get; set; }
        public bool CanTeachOffline { get; set; }
                public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}