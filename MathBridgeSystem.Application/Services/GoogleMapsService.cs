using MathBridge.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MathBridge.Application.DTOs;

namespace MathBridge.Infrastructure.Services;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleMapsService> _logger;
    private readonly string _apiKey;

    public GoogleMapsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GoogleMapsService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _apiKey = _configuration["GoogleMaps:ApiKey"] ?? throw new InvalidOperationException("Google Maps API key not configured");
        
        _httpClient.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<AddressAutocompleteResponse> GetPlaceAutocompleteAsync(string input, string? country = null)
    {
        try
        {
            var queryParams = $"place/autocomplete/json?input={Uri.EscapeDataString(input)}&key={_apiKey}&types=address";
            
            if (!string.IsNullOrEmpty(country))
            {
                queryParams += $"&components=country:{country}";
            }

            _logger.LogInformation("Calling Google Maps Autocomplete API with input: {Input}", input);

            var response = await _httpClient.GetAsync(queryParams);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Maps API returned status code: {StatusCode}", response.StatusCode);
                return new AddressAutocompleteResponse
                {
                    Success = false,
                    ErrorMessage = $"Google Maps API returned status code: {response.StatusCode}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var googleResponse = JsonSerializer.Deserialize<GoogleAutocompleteResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (googleResponse == null)
            {
                return new AddressAutocompleteResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to parse Google Maps API response"
                };
            }

            if (googleResponse.Status != "OK" && googleResponse.Status != "ZERO_RESULTS")
            {
                _logger.LogError("Google Maps API returned error status: {Status}, Message: {ErrorMessage}", 
                    googleResponse.Status, googleResponse.ErrorMessage);
                
                return new AddressAutocompleteResponse
                {
                    Success = false,
                    ErrorMessage = googleResponse.ErrorMessage ?? $"Google Maps API error: {googleResponse.Status}"
                };
            }

            var predictions = googleResponse.Predictions?.Select(p => new AddressPrediction
            {
                PlaceId = p.PlaceId ?? "",
                Description = p.Description ?? "",
                MainText = p.StructuredFormatting?.MainText ?? "",
                SecondaryText = p.StructuredFormatting?.SecondaryText ?? "",
                Types = p.Types ?? new List<string>()
            }).ToList() ?? new List<AddressPrediction>();

            _logger.LogInformation("Google Maps Autocomplete returned {Count} predictions", predictions.Count);

            return new AddressAutocompleteResponse
            {
                Success = true,
                Predictions = predictions
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Google Maps Autocomplete API");
            return new AddressAutocompleteResponse
            {
                Success = false,
                ErrorMessage = "Network error calling Google Maps API"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Google Maps Autocomplete API");
            return new AddressAutocompleteResponse
            {
                Success = false,
                ErrorMessage = "Timeout calling Google Maps API"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Google Maps Autocomplete API");
            return new AddressAutocompleteResponse
            {
                Success = false,
                ErrorMessage = "Unexpected error calling Google Maps API"
            };
        }
    }

    public async Task<PlaceDetailsResponse> GetPlaceDetailsAsync(string placeId)
    {
        try
        {
            var queryParams = $"place/details/json?place_id={Uri.EscapeDataString(placeId)}&key={_apiKey}&fields=place_id,formatted_address,geometry,address_components,name";

            _logger.LogInformation("Calling Google Maps Place Details API for place: {PlaceId}", placeId);

            var response = await _httpClient.GetAsync(queryParams);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Google Maps Place Details API returned status code: {StatusCode}", response.StatusCode);
                return new PlaceDetailsResponse
                {
                    Success = false,
                    ErrorMessage = $"Google Maps API returned status code: {response.StatusCode}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var googleResponse = JsonSerializer.Deserialize<GooglePlaceDetailsResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (googleResponse == null || googleResponse.Result == null)
            {
                return new PlaceDetailsResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to parse Google Maps Place Details response"
                };
            }

            if (googleResponse.Status != "OK")
            {
                _logger.LogError("Google Maps Place Details API returned error status: {Status}", googleResponse.Status);
                return new PlaceDetailsResponse
                {
                    Success = false,
                    ErrorMessage = $"Google Maps API error: {googleResponse.Status}"
                };
            }

            var result = googleResponse.Result;
            var addressComponents = ParseAddressComponents(result.AddressComponents ?? new List<GoogleAddressComponent>());

            var placeDetails = new PlaceDetails
            {
                PlaceId = result.PlaceId ?? "",
                FormattedAddress = result.FormattedAddress ?? "",
                Latitude = result.Geometry?.Location?.Lat ?? 0,
                Longitude = result.Geometry?.Location?.Lng ?? 0,
                PlaceName = result.Name,
                City = addressComponents.City,
                District = addressComponents.District,
                CountryCode = addressComponents.CountryCode,
                AddressComponents = result.AddressComponents?.Select(ac => new AddressComponent
                {
                    LongName = ac.LongName ?? "",
                    ShortName = ac.ShortName ?? "",
                    Types = ac.Types ?? new List<string>()
                }).ToList() ?? new List<AddressComponent>()
            };

            _logger.LogInformation("Successfully retrieved place details for: {PlaceId}", placeId);

            return new PlaceDetailsResponse
            {
                Success = true,
                Place = placeDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google Maps Place Details API for place: {PlaceId}", placeId);
            return new PlaceDetailsResponse
            {
                Success = false,
                ErrorMessage = "Error calling Google Maps Place Details API"
            };
        }
    }

    public static (string? City, string? District, string? CountryCode) ParseAddressComponents(List<GoogleAddressComponent> components)
    {
        string? city = null, district = null, countryCode = null;

        foreach (var component in components)
        {
            var types = component.Types ?? new List<string>();

            if (types.Contains("administrative_area_level_1"))
                city = component.LongName;
            else if (types.Contains("administrative_area_level_2"))
                district = component.LongName;
            else if (types.Contains("country"))
                countryCode = component.ShortName;
        }

        return (city, district, countryCode);
    }
}

// Google Maps API Response Models
public class GoogleAutocompleteResponse
{
    public List<GooglePrediction>? Predictions { get; set; }
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

public class GooglePrediction
{
    public string? PlaceId { get; set; }
    public string? Description { get; set; }
    public List<string>? Types { get; set; }
    public GoogleStructuredFormatting? StructuredFormatting { get; set; }
}

public class GoogleStructuredFormatting
{
    public string? MainText { get; set; }
    public string? SecondaryText { get; set; }
}

public class GooglePlaceDetailsResponse
{
    public GooglePlaceResult? Result { get; set; }
    public string Status { get; set; } = "";
}

public class GooglePlaceResult
{
    public string? PlaceId { get; set; }
    public string? FormattedAddress { get; set; }
    public string? Name { get; set; }
    public GoogleGeometry? Geometry { get; set; }
    public List<GoogleAddressComponent>? AddressComponents { get; set; }
}

public class GoogleGeometry
{
    public GoogleLocation? Location { get; set; }
}

public class GoogleLocation
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class GoogleAddressComponent
{
    public string? LongName { get; set; }
    public string? ShortName { get; set; }
    public List<string>? Types { get; set; }
}