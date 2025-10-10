using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs
{
    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "FullName is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "FullName must be between 1 and 255 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "PhoneNumber is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "PhoneNumber must be up to 20 characters")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(male|female|other)$", ErrorMessage = "Gender must be 'male', 'female', or 'other'")]
        public string Gender { get; set; }
    }
}