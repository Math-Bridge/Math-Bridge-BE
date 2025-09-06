using System.ComponentModel.DataAnnotations;

namespace MathBridge.Application.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "FullName is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "FullName must be between 1 and 255 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email must be up to 255 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "PhoneNumber is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "PhoneNumber must be up to 20 characters")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(male|female|other)$", ErrorMessage = "Gender must be 'male', 'female', or 'other'")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "RoleId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "RoleId must be a positive integer")]
        public int RoleId { get; set; }
    }
}