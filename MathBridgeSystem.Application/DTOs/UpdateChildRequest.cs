using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class UpdateChildRequest
    {
        public string FullName { get; set; }
        public Guid SchoolId { get; set; }
        public Guid? CenterId { get; set; }
        public string Grade { get; set; }
        public DateOnly? DateOfBirth { get; set; }
    }
}
