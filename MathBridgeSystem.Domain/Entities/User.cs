using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public decimal WalletBalance { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActive { get; set; }
        public string Status { get; set; }
        public int RoleId { get; set; }
        public Role Role { get; set; }
        public List<WalletTransaction> WalletTransactions { get; set; }

        // Location fields
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? PlaceName { get; set; }
        public string? CountryCode { get; set; }
        public DateTime? LocationUpdatedDate { get; set; }
    }
}