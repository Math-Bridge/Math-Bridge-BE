using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class UpdateSessionStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = null!;
    }
}
