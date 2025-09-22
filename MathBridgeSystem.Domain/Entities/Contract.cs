using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathBridge.Domain.Entities;

public partial class Contract
{
    public Guid ContractId { get; set; }
    public Guid ParentId { get; set; }
    public Guid ChildId { get; set; }
    public Guid? CenterId { get; set; }
    public Guid PackageId { get; set; }
    public Guid MainTutorId { get; set; }
    public Guid? SubstituteTutor1Id { get; set; }
    public Guid? SubstituteTutor2Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TimeSlot { get; set; } = null!;
    public bool IsOnline { get; set; }
    public string? OfflineAddress { get; set; }
    public decimal? OfflineLatitude { get; set; }
    public decimal? OfflineLongitude { get; set; }
    public string? VideoCallPlatform { get; set; }
    public decimal MaxDistanceKm { get; set; }
    public int RescheduleCount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    [ForeignKey("ParentId")]
    public virtual User Parent { get; set; } = null!;

    [ForeignKey("ChildId")]
    public virtual Child Child { get; set; } = null!;

    [ForeignKey("PackageId")]
    public virtual PaymentPackage Package { get; set; } = null!;

    [ForeignKey("MainTutorId")]
    public virtual User MainTutor { get; set; } = null!;

    [ForeignKey("SubstituteTutor1Id")]
    public virtual User? SubstituteTutor1 { get; set; }

    [ForeignKey("SubstituteTutor2Id")]
    public virtual User? SubstituteTutor2 { get; set; }

    [ForeignKey("CenterId")]
    public virtual Center? Center { get; set; }

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}