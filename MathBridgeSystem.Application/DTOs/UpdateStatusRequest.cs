using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs
{
    public class UpdateStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(active|inactive|deleted|banned)$", ErrorMessage = "Status not found. Allowed values are: active, inactive, deleted")]
        public string Status { get; set; }
    }
}