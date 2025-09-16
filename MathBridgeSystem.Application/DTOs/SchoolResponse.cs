using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class SchoolResponse
    {
        public Guid SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PlaceName { get; set; }
        public string? CountryCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? LocationUpdatedDate { get; set; }
    }
}