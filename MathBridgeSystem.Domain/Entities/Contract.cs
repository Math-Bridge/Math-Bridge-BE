using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class Contract
{
    public Guid ContractId { get; set; }

    public Guid ParentId { get; set; }

    public Guid ChildId { get; set; }

    public Guid PackageId { get; set; }

    public Guid? MainTutorId { get; set; }

    public Guid? SubstituteTutor1Id { get; set; }

    public Guid? SubstituteTutor2Id { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsOnline { get; set; }

    public string? OfflineAddress { get; set; }

    public decimal? OfflineLatitude { get; set; }

    public decimal? OfflineLongitude { get; set; }

    public string? VideoCallPlatform { get; set; }

    public decimal MaxDistanceKm { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? CenterId { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public byte? DaysOfWeeks { get; set; }

    public int? RescheduleCount { get; set; }

    public virtual Center? Center { get; set; }

    public virtual Child Child { get; set; } = null!;

    public virtual ICollection<FinalFeedback> FinalFeedbacks { get; set; } = new List<FinalFeedback>();

    public virtual User? MainTutor { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual PaymentPackage Package { get; set; } = null!;

    public virtual User Parent { get; set; } = null!;

    public virtual ICollection<RescheduleRequest> RescheduleRequests { get; set; } = new List<RescheduleRequest>();

    public virtual ICollection<SepayTransaction> SepayTransactions { get; set; } = new List<SepayTransaction>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual User? SubstituteTutor1 { get; set; }

    public virtual User? SubstituteTutor2 { get; set; }

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();

    public virtual ICollection<VideoConferenceSession> VideoConferenceSessions { get; set; } = new List<VideoConferenceSession>();

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
