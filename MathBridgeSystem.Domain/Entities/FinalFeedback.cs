using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class FinalFeedback
{
    public Guid FeedbackId { get; set; }

    public Guid UserId { get; set; }

    public Guid ContractId { get; set; }

    public string FeedbackProviderType { get; set; } = null!;

    public string? FeedbackText { get; set; }

    public int OverallSatisfactionRating { get; set; }

    public int? CommunicationRating { get; set; }

    public int? SessionQualityRating { get; set; }

    public int? LearningProgressRating { get; set; }

    public int? ProfessionalismRating { get; set; }

    public bool WouldRecommend { get; set; }

    public bool WouldWorkTogetherAgain { get; set; }

    public bool? ContractObjectivesMet { get; set; }

    public string? ImprovementSuggestions { get; set; }

    public string? AdditionalComments { get; set; }

    public string FeedbackStatus { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
