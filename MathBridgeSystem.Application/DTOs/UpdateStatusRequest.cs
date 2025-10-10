using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs
{
    public class UpdateStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        [RegularExpression("^(active|banned)$", ErrorMessage = "Status must be 'active' or 'banned'")]
        public string Status { get; set; }
    }
}