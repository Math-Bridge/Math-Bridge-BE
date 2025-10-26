using System;

namespace MathBridgeSystem.Application.DTOs.TutorSchedule
{
    public class CreateTutorScheduleRequest
    {
        public Guid TutorId { get; set; }
        public byte DaysOfWeek { get; set; }
        public TimeOnly AvailableFrom { get; set; }
        public TimeOnly AvailableUntil { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveUntil { get; set; }
        public bool isBooked { get; set; } = false;
        public bool CanTeachOnline { get; set; } = true;
        public bool CanTeachOffline { get; set; } = true;
            }
}