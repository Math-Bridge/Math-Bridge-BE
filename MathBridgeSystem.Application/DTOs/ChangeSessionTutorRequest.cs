using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class ChangeSessionTutorRequest
    {
        [Required] public Guid BookingId { get; set; }       
        [Required] public Guid NewTutorId { get; set; }       
    }
}
