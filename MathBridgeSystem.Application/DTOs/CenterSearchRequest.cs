
namespace MathBridgeSystem.Application.DTOs
{
    public class CenterSearchRequest
    {
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; } = 10.0;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}