using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs;

public class SaveAddressRequest
{
    [Required(ErrorMessage = "PlaceId is required")]
    public string PlaceId { get; set; }
}