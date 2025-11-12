using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MathBridgeSystem.Application.DTOs
{
    public class CreatePackageRequest
    {
        public string PackageName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public decimal Price { get; set; }
        public int SessionsPerWeek { get; set; }
        public int MaxReschedule { get; set; }
        public string? Description { get; set; }
        public Guid CurriculumId { get; set; } 
        public bool IsActive { get; set; }
    }
}