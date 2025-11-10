using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.FinalFeedback
{
    public class UpdateFinalFeedbackRequest
    {
        [StringLength(1000)]
        public string? FeedbackText { get; set; }
        
        [Range(1, 5)]
        public int? OverallSatisfactionRating { get; set; }
        
        [Range(1, 5)]
        public int? CommunicationRating { get; set; }
        
        [Range(1, 5)]
        public int? SessionQualityRating { get; set; }
        
        [Range(1, 5)]
        public int? LearningProgressRating { get; set; }
        
        [Range(1, 5)]
        public int? ProfessionalismRating { get; set; }
        
        public bool? WouldRecommend { get; set; }
        
        public bool? WouldWorkTogetherAgain { get; set; }
        
        public bool? ContractObjectivesMet { get; set; }
        
        [StringLength(500)]
        public string? ImprovementSuggestions { get; set; }
        
        [StringLength(1000)]
        public string? AdditionalComments { get; set; }
        
        [StringLength(50)]
        public string? FeedbackStatus { get; set; }
    }
}