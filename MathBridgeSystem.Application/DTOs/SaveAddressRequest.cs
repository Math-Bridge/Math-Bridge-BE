using System.ComponentModel.DataAnnotations;

namespace MathBridge.Application.DTOs;

public class SaveAddressRequest
{
    [Required(ErrorMessage = "PlaceId is required")]
    public string PlaceId { get; set; }

    [Required(ErrorMessage = "FormattedAddress is required")]
    [StringLength(500, ErrorMessage = "FormattedAddress must be up to 500 characters")]
    public string FormattedAddress { get; set; }

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? PlaceName { get; set; }
    public string? CountryCode { get; set; }
}