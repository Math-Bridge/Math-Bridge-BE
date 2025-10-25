using System;

namespace MathBridgeSystem.Application.DTOs.TutorSchedule
{
    public class CreateTutorScheduleRequest
    {
        public Guid TutorId { get; set; }
        public byte DayOfWeek { get; set; }
        public TimeOnly AvailableFrom { get; set; }
        public TimeOnly AvailableUntil { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveUntil { get; set; }
        public int MaxConcurrentBookings { get; set; } = 1;
        public bool CanTeachOnline { get; set; } = true;
        public bool CanTeachOffline { get; set; } = true;
            }
}