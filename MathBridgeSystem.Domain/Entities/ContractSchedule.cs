using System;

namespace MathBridgeSystem.Domain.Entities
{
    public class ContractSchedule
    {
        public Guid ScheduleId { get; set; } = Guid.NewGuid();
        public Guid ContractId { get; set; }
        public DayOfWeek DayOfWeek { get; set; } // 1 = Monday, 2 = Tuesday,...
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public virtual Contract Contract { get; set; } = null!;
    }
}