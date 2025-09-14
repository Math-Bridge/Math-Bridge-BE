using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class VerifyEmailRequest
    {
        [Required(ErrorMessage = "The oobCode field is required")]
        public string OobCode { get; set; }
    }
}
