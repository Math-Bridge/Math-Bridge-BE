using System;

namespace MathBridgeSystem.Application.DTOs.TutorAvailability
{
    public class SearchAvailableTutorsRequest
    {
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool? CanTeachOnline { get; set; }
        public bool? CanTeachOffline { get; set; }
        public DateTime EffectiveDate { get; set; }
        public decimal? MaxTravelDistanceKm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}