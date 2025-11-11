using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.Statistics
{
    public class TopRatedTutorDto
    {
        public Guid TutorId { get; set; }
        public string TutorName { get; set; } = null!;
        public decimal AverageRating { get; set; }
        public int FeedbackCount { get; set; }
        public string Email { get; set; } = null!;
    }

    public class TutorSessionCountDto
    {
        public Guid TutorId { get; set; }
        public string TutorName { get; set; } = null!;
        public int SessionCount { get; set; }
        public int CompletedSessions { get; set; }
        public string Email { get; set; } = null!;
    }

    public class TutorStatisticsDto
    {
        public int TotalTutors { get; set; }
        public decimal AverageRating { get; set; }
        public int TutorsWithFeedback { get; set; }
        public int TutorsWithoutFeedback { get; set; }
    }

    public class TopRatedTutorsListDto
    {
        public List<TopRatedTutorDto> Tutors { get; set; } = new List<TopRatedTutorDto>();
        public int TotalTutors { get; set; }
    }

    public class MostActiveTutorsListDto
    {
        public List<TutorSessionCountDto> Tutors { get; set; } = new List<TutorSessionCountDto>();
        public int TotalTutors { get; set; }
    }
}

