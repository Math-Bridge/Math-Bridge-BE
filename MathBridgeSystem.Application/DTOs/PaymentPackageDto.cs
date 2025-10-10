using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class PaymentPackageDto
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; }
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string Grade { get; set; }
        public decimal Price { get; set; }
        public int SessionCount { get; set; }
        public int SessionsPerWeek { get; set; }
        public int MaxReschedule { get; set; }
        public int DurationDays { get; set; }
        public string? Description { get; set; }
    }
}
