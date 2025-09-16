using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

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

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
