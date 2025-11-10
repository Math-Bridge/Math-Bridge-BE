using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Review
{
    public class ReviewDto
    {
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? ReviewTitle { get; set; }
        public string ReviewText { get; set; } = string.Empty;
        public string ReviewStatus { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class CreateReviewRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(200)]
        public string? ReviewTitle { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ReviewText { get; set; } = string.Empty;
    }

    public class UpdateReviewRequest
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int? Rating { get; set; }

        [MaxLength(200)]
        public string? ReviewTitle { get; set; }

        [MaxLength(1000)]
        public string? ReviewText { get; set; }

        [MaxLength(50)]
        public string? ReviewStatus { get; set; }
    }
}