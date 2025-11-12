using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.Progress
{
    public class ChildUnitProgressDto
    {
        public Guid ChildId { get; set; }

        public string ChildName { get; set; } = null!;

        public Guid CurriculumId { get; set; }

        public string CurriculumName { get; set; } = null!;

        public int TotalUnitsLearned { get; set; }

        public int UniqueLessonsCompleted { get; set; }

        public List<UnitProgressDetail> UnitsProgress { get; set; } = new();

        public DateOnly FirstLessonDate { get; set; }

        public DateOnly LastLessonDate { get; set; }

        public int TotalLessonDays { get; set; }

        public double AverageUnitsPerWeek { get; set; }

        public string Message { get; set; } = null!;
    }

    public class UnitProgressDetail
    {
        public Guid UnitId { get; set; }

        public string UnitName { get; set; } = null!;

        public int UnitOrder { get; set; }

        public int TimesLearned { get; set; }

        public DateOnly FirstLearned { get; set; }

        public DateOnly LastLearned { get; set; }

        public int DaysSinceLearned { get; set; }

        public bool OnTrack { get; set; }

        public bool HasHomework { get; set; }
    }
}

