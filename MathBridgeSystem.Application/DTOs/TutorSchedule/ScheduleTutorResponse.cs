using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.TutorSchedule
{
    public class AvailableTutorResponse
    {
        public Guid TutorId { get; set; }
        public string TutorName { get; set; }
        public string TutorEmail { get; set; }
        public List<TutorScheduleResponse> AvailabilitySlots { get; set; }
        public int TotalAvailableSlots => AvailabilitySlots?.Count ?? 0;
        public bool CanTeachOnline { get; set; }
        public bool CanTeachOffline { get; set; }
    }
}