using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.TutorAvailability
{
    public class AvailableTutorResponse
    {
        public Guid TutorId { get; set; }
        public string TutorName { get; set; }
        public string TutorEmail { get; set; }
        public decimal? HourlyRate { get; set; }
        public List<TutorAvailabilityResponse> AvailabilitySlots { get; set; }
        public int TotalAvailableSlots { get; set; }
        public bool CanTeachOnline { get; set; }
        public bool CanTeachOffline { get; set; }
    }
}