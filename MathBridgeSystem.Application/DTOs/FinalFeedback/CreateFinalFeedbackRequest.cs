using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.FinalFeedback
{
    public class CreateFinalFeedbackRequest
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public Guid ContractId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FeedbackProviderType { get; set; } = null!;
        
        [StringLength(1000)]
        public string? FeedbackText { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int OverallSatisfactionRating { get; set; }
        
        [Range(1, 5)]
        public int? CommunicationRating { get; set; }
        
        [Range(1, 5)]
        public int? SessionQualityRating { get; set; }
        
        [Range(1, 5)]
        public int? LearningProgressRating { get; set; }
        
        [Range(1, 5)]
        public int? ProfessionalismRating { get; set; }
        
        [Required]
        public bool WouldRecommend { get; set; }
        
        [Required]
        public bool WouldWorkTogetherAgain { get; set; }
        
        public bool? ContractObjectivesMet { get; set; }
        
        [StringLength(500)]
        public string? ImprovementSuggestions { get; set; }
        
        [StringLength(1000)]
        public string? AdditionalComments { get; set; }
    }
}