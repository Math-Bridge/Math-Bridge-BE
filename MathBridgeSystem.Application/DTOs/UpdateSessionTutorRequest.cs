using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs
{
    public class UpdateSessionTutorRequest
    {
        [Required(ErrorMessage = "New tutor ID is required")]
        public Guid NewTutorId { get; set; }
    }
}
