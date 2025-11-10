using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.TestResult
{
    public class TestResultDto
    {
        public Guid ResultId { get; set; }
        public Guid TutorId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public Guid ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public string TestName { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? Percentage { get; set; }
        public int? DurationMinutes { get; set; }
        public int? NumberOfQuestions { get; set; }
        public int? CorrectAnswers { get; set; }
        public string? Notes { get; set; }
        public string? AreasForImprovement { get; set; }
        public DateTime TestDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Guid CurriculumId { get; set; }
        public string CurriculumName { get; set; } = string.Empty;
    }

    public class CreateTestResultRequest
    {
        [Required]
        public Guid ChildId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TestName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TestType { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Score { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal MaxScore { get; set; }

        [Range(0, int.MaxValue)]
        public int? DurationMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int? NumberOfQuestions { get; set; }

        [Range(0, int.MaxValue)]
        public int? CorrectAnswers { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(1000)]
        public string? AreasForImprovement { get; set; }

        [Required]
        public DateTime TestDate { get; set; }

        [Required]
        public Guid CurriculumId { get; set; }
    }

    public class UpdateTestResultRequest
    {
        [MaxLength(200)]
        public string? TestName { get; set; }

        [MaxLength(100)]
        public string? TestType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Score { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxScore { get; set; }

        [Range(0, int.MaxValue)]
        public int? DurationMinutes { get; set; }

        [Range(0, int.MaxValue)]
        public int? NumberOfQuestions { get; set; }

        [Range(0, int.MaxValue)]
        public int? CorrectAnswers { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(1000)]
        public string? AreasForImprovement { get; set; }

        public DateTime? TestDate { get; set; }

        public Guid? CurriculumId { get; set; }
    }

    public class TestResultStatisticsDto
    {
        public Guid ChildId { get; set; }
        public int TotalTests { get; set; }
        public decimal AverageScore { get; set; }
        public decimal AveragePercentage { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public DateTime? LastTestDate { get; set; }
    }
}