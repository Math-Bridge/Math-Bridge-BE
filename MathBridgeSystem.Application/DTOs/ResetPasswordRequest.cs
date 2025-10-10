using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs
{
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "OobCode is required")]
        public string OobCode { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
    ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string NewPassword { get; set; }
    }
}