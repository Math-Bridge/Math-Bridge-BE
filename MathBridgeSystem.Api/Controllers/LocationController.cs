using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}