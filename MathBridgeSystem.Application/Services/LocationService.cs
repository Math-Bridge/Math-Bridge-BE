using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathBridge.Application.Services;

public class LocationService : ILocationService
{
    private readonly IGoogleMapsService _googleMapsService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        IGoogleMapsService googleMapsService,
        IUserRepository userRepository,
        ILogger<LocationService> logger)
    {
        _googleMapsService = googleMapsService ?? throw new ArgumentNullException(nameof(googleMapsService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AddressAutocompleteResponse> GetAddressAutocompleteAsync(string input, string? country = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new AddressAutocompleteResponse
                {
                    Success = false,
                    ErrorMessage = "Input is required for address autocomplete"
                };
            }

            _logger.LogInformation("Getting address autocomplete for input: {Input}", input);
            
            var result = await _googleMapsService.GetPlaceAutocompleteAsync(input, country);
            
            _logger.LogInformation("Address autocomplete returned {Count} predictions", result.Predictions.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address autocomplete for input: {Input}", input);
            return new AddressAutocompleteResponse
            {
                Success = false,
                ErrorMessage = "Failed to get address suggestions"
            };
        }
    }

    public async Task<SaveAddressResponse> SaveUserAddressAsync(Guid userId, SaveAddressRequest request)
    {
        try
        {
            _logger.LogInformation("Saving address for user: {UserId}", userId);

            // Get detailed place information from Google Maps
            var placeDetails = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
            
            if (!placeDetails.Success || placeDetails.Place == null)
            {
                return new SaveAddressResponse
                {
                    Success = false,
                    Message = "Failed to get place details from Google Maps"
                };
            }

            // Update user location in database
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new SaveAddressResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Update user location fields
            user.GooglePlaceId = request.PlaceId;
            user.FormattedAddress = placeDetails.Place.FormattedAddress;
            user.Latitude = placeDetails.Place.Latitude;
            user.Longitude = placeDetails.Place.Longitude;
            user.City = placeDetails.Place.City;
            user.District = placeDetails.Place.District;
            user.PlaceName = placeDetails.Place.PlaceName;
            user.CountryCode = placeDetails.Place.CountryCode;
            user.LocationUpdatedDate = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Successfully saved address for user: {UserId}", userId);

            return new SaveAddressResponse
            {
                Success = true,
                Message = "Address saved successfully",
                LocationUpdatedDate = user.LocationUpdatedDate.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving address for user: {UserId}", userId);
            return new SaveAddressResponse
            {
                Success = false,
                Message = "Failed to save address"
            };
        }
    }

    public async Task<FindNearbyUsersResponse> FindNearbyUsersAsync(Guid currentUserId, int radiusKm = 5)
    {
        try
        {
            _logger.LogInformation("Finding nearby users for user: {UserId} within {RadiusKm}km", currentUserId, radiusKm);

            // Get current user's location
            var currentUser = await _userRepository.GetByIdAsync(currentUserId);
            if (currentUser == null)
            {
                return new FindNearbyUsersResponse
                {
                    Success = false,
                    ErrorMessage = "Current user not found"
                };
            }

            if (!currentUser.Latitude.HasValue || !currentUser.Longitude.HasValue)
            {
                return new FindNearbyUsersResponse
                {
                    Success = false,
                    ErrorMessage = "Current user location not set. Please save your address first."
                };
            }

            // Find nearby users using Haversine formula
            var allUsers = await _userRepository.GetUsersWithLocationAsync();
            var nearbyUsers = new List<NearbyUser>();

            foreach (var user in allUsers)
            {
                if (user.UserId == currentUserId) continue; // Skip current user
                
                if (user.Latitude.HasValue && user.Longitude.HasValue)
                {
                    var distance = CalculateDistance(
                        currentUser.Latitude.Value, 
                        currentUser.Longitude.Value,
                        user.Latitude.Value,
                        user.Longitude.Value);

                    if (distance <= radiusKm)
                    {
                        nearbyUsers.Add(new NearbyUser
                        {
                            UserId = user.UserId,
                            FullName = user.FullName,
                            FormattedAddress = user.FormattedAddress ?? "",
                            Latitude = user.Latitude.Value,
                            Longitude = user.Longitude.Value,
                            DistanceKm = Math.Round(distance, 2),
                            City = user.City,
                            District = user.District,
                        });
                    }
                }
            }

            // Sort by distance
            nearbyUsers = nearbyUsers.OrderBy(u => u.DistanceKm).ToList();

            _logger.LogInformation("Found {Count} nearby users within {RadiusKm}km for user: {UserId}", 
                nearbyUsers.Count, radiusKm, currentUserId);

            return new FindNearbyUsersResponse
            {
                Success = true,
                NearbyUsers = nearbyUsers,
                TotalUsers = nearbyUsers.Count,
                RadiusKm = radiusKm
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding nearby users for user: {UserId}", currentUserId);
            return new FindNearbyUsersResponse
            {
                Success = false,
                ErrorMessage = "Failed to find nearby users"
            };
        }
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate distance between two points on Earth
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}