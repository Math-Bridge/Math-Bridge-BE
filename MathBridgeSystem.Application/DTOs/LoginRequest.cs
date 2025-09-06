using System.ComponentModel.DataAnnotations;

namespace MathBridge.Application.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Wrong fomat")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be than 6 charaters")]
        public string Password { get; set; }
    }
}