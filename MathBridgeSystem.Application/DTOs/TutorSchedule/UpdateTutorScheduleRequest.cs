using System;

namespace MathBridgeSystem.Application.DTOs.TutorSchedule
{
    public class UpdateTutorScheduleRequest
    {
        public byte? DaysOfWeek { get; set; }
        public TimeOnly? AvailableFrom { get; set; }
        public TimeOnly? AvailableUntil { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveUntil { get; set; }
        public bool? CanTeachOnline { get; set; }
        public bool? CanTeachOffline { get; set; }
    }
}