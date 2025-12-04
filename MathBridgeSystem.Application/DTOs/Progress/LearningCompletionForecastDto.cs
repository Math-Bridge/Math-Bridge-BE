using System;

namespace MathBridgeSystem.Application.DTOs.Progress
{
    public class LearningCompletionForecastDto
    {
        public Guid ContractId { get; set; }

        public Guid ChildId { get; set; }

        public string ChildName { get; set; } = null!;

        public Guid CurriculumId { get; set; }

        public string CurriculumName { get; set; } = null!;

        public Guid PackageId { get; set; }

        public string PackageName { get; set; } = null!;

        public Guid CurrentUnitId { get; set; }

        public string CurrentUnitName { get; set; } = null!;

        public int CurrentUnitOrder { get; set; }

        public Guid EstimatedLastUnitId { get; set; }

        public string EstimatedLastUnitName { get; set; } = null!;

        public int EstimatedLastUnitOrder { get; set; }

        public int TotalUnitsToComplete { get; set; }

        public int CompletedSessions { get; set; }

        public int TotalSessions { get; set; }

        public int RemainingSessions { get; set; }

        public DateOnly ContractStartDate { get; set; }

        public DateOnly ContractEndDate { get; set; }

        public DateOnly? FirstLessonDate { get; set; }

        public DateOnly? LastLessonDate { get; set; }

        public DateTime EstimatedCompletionDate { get; set; }

        public int DaysToCompletion { get; set; }

        public double WeeksToCompletion { get; set; }

        public double ProgressPercentage { get; set; }

        public string Message { get; set; } = null!;
    }
}
