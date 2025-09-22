using MathBridge.Application.DTOs;

namespace MathBridge.Application.Interfaces;

public interface IGoogleMapsService
{
    Task<AddressAutocompleteResponse> GetPlaceAutocompleteAsync(string input, string? country = null);
    Task<PlaceDetailsResponse> GetPlaceDetailsAsync(string placeId);
    Task<GeocodeResponse> GeocodeAddressAsync(string address, string? country = "VN");
}

public class PlaceDetailsResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PlaceDetails? Place { get; set; }
}

public class PlaceDetails
{
    public string PlaceId { get; set; }
    public string FormattedAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PlaceName { get; set; }
    public string? CountryCode { get; set; }
    public List<AddressComponent> AddressComponents { get; set; } = new();
}

public class AddressComponent
{
    public string LongName { get; set; }
    public string ShortName { get; set; }
    public List<string> Types { get; set; } = new();
}