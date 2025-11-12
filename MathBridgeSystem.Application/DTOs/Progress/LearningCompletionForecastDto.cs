using System;

namespace MathBridgeSystem.Application.DTOs.Progress
{
    public class LearningCompletionForecastDto
    {
        public Guid ChildId { get; set; }

        public string ChildName { get; set; } = null!;

        public Guid CurriculumId { get; set; }

        public string CurriculumName { get; set; } = null!;

        public Guid StartingUnitId { get; set; }

        public string StartingUnitName { get; set; } = null!;

        public int StartingUnitOrder { get; set; }

        public Guid LastUnitId { get; set; }

        public string LastUnitName { get; set; } = null!;

        public int LastUnitOrder { get; set; }

        public int TotalUnitsToComplete { get; set; }

        public DateOnly StartDate { get; set; }

        public DateTime EstimatedCompletionDate { get; set; }

        public int DaysToCompletion { get; set; }

        public double WeeksToCompletion { get; set; }

        public string Message { get; set; } = null!;
    }
}
