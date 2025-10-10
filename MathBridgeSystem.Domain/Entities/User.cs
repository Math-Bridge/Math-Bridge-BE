using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public decimal WalletBalance { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime LastActive { get; set; }

    public string Status { get; set; } = null!;

    public int RoleId { get; set; }

    /// <summary>
    /// Google Places API place identifier for location accuracy
    /// </summary>
    public string? GooglePlaceId { get; set; }

    /// <summary>
    /// Complete formatted address from Google Places API
    /// </summary>
    public string? FormattedAddress { get; set; }

    /// <summary>
    /// GPS latitude coordinate for distance calculations
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// GPS longitude coordinate for distance calculations
    /// </summary>
    public double? Longitude { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? PlaceName { get; set; }

    public string? CountryCode { get; set; }

    public DateTime? LocationUpdatedDate { get; set; }

    public virtual ICollection<ChatMessage> ChatMessageRecipientUsers { get; set; } = new List<ChatMessage>();

    public virtual ICollection<ChatMessage> ChatMessageUsers { get; set; } = new List<ChatMessage>();

    public virtual ICollection<Child> Children { get; set; } = new List<Child>();

    public virtual ICollection<Contract> ContractMainTutors { get; set; } = new List<Contract>();

    public virtual ICollection<Contract> ContractParents { get; set; } = new List<Contract>();

    public virtual ICollection<Contract> ContractSubstituteTutor1s { get; set; } = new List<Contract>();

    public virtual ICollection<Contract> ContractSubstituteTutor2s { get; set; } = new List<Contract>();

    public virtual ICollection<FinalFeedback> FinalFeedbacks { get; set; } = new List<FinalFeedback>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<RescheduleRequest> RescheduleRequestParents { get; set; } = new List<RescheduleRequest>();

    public virtual ICollection<RescheduleRequest> RescheduleRequestRequestedTutors { get; set; } = new List<RescheduleRequest>();

    public virtual ICollection<RescheduleRequest> RescheduleRequestStaffs { get; set; } = new List<RescheduleRequest>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<SupportRequest> SupportRequestAssignedToUsers { get; set; } = new List<SupportRequest>();

    public virtual ICollection<SupportRequest> SupportRequestUsers { get; set; } = new List<SupportRequest>();

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();

    public virtual ICollection<TutorAvailability> TutorAvailabilities { get; set; } = new List<TutorAvailability>();

    public virtual ICollection<TutorCenter> TutorCenters { get; set; } = new List<TutorCenter>();

    public virtual TutorVerification? TutorVerification { get; set; }

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
