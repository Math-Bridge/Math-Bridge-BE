using System.ComponentModel.DataAnnotations;

namespace MathBridge.Application.DTOs
{
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google ID token is required")]
        public string IdToken { get; set; }
    }
}