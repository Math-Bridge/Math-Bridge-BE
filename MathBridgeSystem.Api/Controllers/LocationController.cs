using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MathBridge.Presentation.Controllers;

[Route("api/location")]
[ApiController]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService locationService, ILogger<LocationController> logger)
    {
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get address autocomplete suggestions from Google Maps
    /// </summary>
    /// <param name="input">Search query for address</param>
    /// <param name="country">Country code to restrict results (e.g., 'VN' for Vietnam)</param>
    /// <returns>List of address predictions</returns>
    [HttpGet("autocomplete")]
    public async Task<IActionResult> GetAddressAutocomplete([FromQuery] string input, [FromQuery] string? country = "VN")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return BadRequest(new { error = "Input parameter is required" });
            }

            _logger.LogInformation("Address autocomplete requested for input: {Input}", input);

            var result = await _locationService.GetAddressAutocompleteAsync(input, country);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                predictions = result.Predictions,
                totalCount = result.Predictions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAddressAutocomplete for input: {Input}", input);
            return StatusCode(500, new { error = "Internal server error while getting address suggestions" });
        }
    }

    /// <summary>
    /// Save selected address to user profile
    /// </summary>
    /// <param name="request">Address details to save</param>
    /// <returns>Save operation result</returns>
    [HttpPost("save-address")]
    public async Task<IActionResult> SaveAddress([FromBody] SaveAddressRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new Exception("Invalid token"));

            _logger.LogInformation("Save address requested for user: {UserId}", currentUserId);

            var result = await _locationService.SaveUserAddressAsync(currentUserId, request);

            if (!result.Success)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                locationUpdatedDate = result.LocationUpdatedDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SaveAddress");

            var errorMessage = string.IsNullOrEmpty(ex.Message)
                ? "Unknown error while saving address"
                : ex.Message;
            return StatusCode(500, new { error = errorMessage });
        }
    }

    /// <summary>
    /// Find nearby users within specified radius
    /// </summary>
    /// <param name="radius">Search radius in kilometers (default: 5km, max: 50km)</param>
    /// <returns>List of nearby users</returns>
    [HttpGet("nearby-users")]
    public async Task<IActionResult> FindNearbyUsers([FromQuery] int radius = 5)
    {
        try
        {
            if (radius <= 0 || radius > 50)
            {
                return BadRequest(new { error = "Radius must be between 1 and 50 kilometers" });
            }

            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new Exception("Invalid token"));

            _logger.LogInformation("Find nearby users requested for user: {UserId} within {Radius}km",
                currentUserId, radius);

            var result = await _locationService.FindNearbyUsersAsync(currentUserId, radius);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                nearbyUsers = result.NearbyUsers,
                totalUsers = result.TotalUsers,
                radiusKm = result.RadiusKm,
                searchedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FindNearbyUsers");

            var errorMessage = string.IsNullOrEmpty(ex.Message)
                ? "Unknown error while finding nearby users"
                : ex.Message;
            return StatusCode(500, new { error = errorMessage });
        }
    }
    /// <summary>
    /// Get centers near a specific address
    /// </summary>
    /// <param name="address">Address to search near</param>
    /// <param name="radiusKm">Search radius in km (default: 10)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Centers within the specified radius</returns>
    [HttpGet("nearby-centers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCentersNearAddress(
        [FromQuery][Required] string address,
        [FromQuery][Range(0.1, 50)] double radiusKm = 10.0,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return BadRequest(new { error = "Address is required." });
            }

            // Validate pagination
            page = Math.Max(1, page);
            pageSize = Math.Min(50, Math.Max(1, pageSize));

            _logger.LogInformation("Finding centers near address: {Address} within {RadiusKm}km, page {Page}, pageSize {PageSize}",
                address, radiusKm, page, pageSize);

            // Get centers using LocationService
            var centers = await _locationService.FindCentersNearAddressAsync(address, radiusKm);

            // Manual pagination
            var totalCount = centers.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var paginatedCenters = centers
                .OrderBy(c => c.City) // Ensure consistent ordering
                .ThenBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get geocoded coordinates for response - Fix Bug Here
            var geocodeResult = await _locationService.GeocodeAddressAsync(address);
            object searchLocation;

            if (geocodeResult.Success && geocodeResult.Latitude.HasValue && geocodeResult.Longitude.HasValue)
            {
                searchLocation = new
                {
                    address = address,
                    latitude = geocodeResult.Latitude,
                    longitude = geocodeResult.Longitude,
                    radiusKm
                };
            }
            else
            {
                searchLocation = new
                {
                    address = address,
                    radiusKm,
                    error = geocodeResult.ErrorMessage
                };
            }

            return Ok(new
            {
                data = paginatedCenters,
                searchLocation = searchLocation,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNext = page < totalPages,
                    hasPrevious = page > 1
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding centers near address: {Address}", address);
            return StatusCode(500, new { error = "An error occurred while retrieving nearby centers." });
        }
    }
}
