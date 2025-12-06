using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class ContractScheduleDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}