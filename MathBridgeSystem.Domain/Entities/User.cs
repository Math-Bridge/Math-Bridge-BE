using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class User
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public decimal WalletBalance { get; set; }
    public int RoleId { get; set; }
    public string Status { get; set; } = null!;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? CountryCode { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastActive { get; set; }
    public string? FormattedAddress { get; set; }
    public string? GooglePlaceId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? LocationUpdatedDate { get; set; }
    public string? PlaceName { get; set; }

    public virtual Role Role { get; set; } = null!;
    public virtual TutorVerification? TutorVerification { get; set; }
    public virtual ICollection<TutorCenter> TutorCenters { get; set; } = new List<TutorCenter>();
    public virtual ICollection<Child> Children { get; set; } = new List<Child>();
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    // Thêm navigation properties cho Contracts
    public virtual ICollection<Contract> ParentContracts { get; set; } = new List<Contract>();
    public virtual ICollection<Contract> MainTutorContracts { get; set; } = new List<Contract>();
    public virtual ICollection<Contract> SubstituteTutor1Contracts { get; set; } = new List<Contract>();
    public virtual ICollection<Contract> SubstituteTutor2Contracts { get; set; } = new List<Contract>();
}