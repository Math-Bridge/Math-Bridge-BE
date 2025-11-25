namespace MathBridgeSystem.Application.DTOs;

public class PlaceCoordinatesResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PlaceId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? FormattedAddress { get; set; }
    public string? PlaceName { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? CountryCode { get; set; }
}
