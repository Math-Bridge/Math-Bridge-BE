using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.TestResult
{
    public class TestResultDto
    {
        public Guid ResultId { get; set; }
        public string TestType { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Guid? ContractId { get; set; }
        public Guid? BookingId { get; set; }
    }

    public class CreateTestResultRequest
    {
        [Required]
        [MaxLength(50)]
        public string TestType { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Score { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public Guid ContractId { get; set; }
        public Guid? BookingId { get; set; }
    }

    public class UpdateTestResultRequest
    {
        [MaxLength(50)]
        public string? TestType { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Score { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public Guid? ContractId { get; set; }
    }
}

