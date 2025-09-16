using System.ComponentModel.DataAnnotations;

namespace MathBridge.Application.DTOs
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "OobCode is required")]
        public string OobCode { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }
    }
}