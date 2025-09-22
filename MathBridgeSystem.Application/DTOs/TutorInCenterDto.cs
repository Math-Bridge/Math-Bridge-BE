using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.DTOs
{
    public class TutorInCenterDto
    {
        public Guid TutorId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public decimal HourlyRate { get; set; }
        public string? Bio { get; set; }
        public string VerificationStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}