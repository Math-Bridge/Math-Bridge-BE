using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.DTOs
{
    public class ContractDto
    {
        public Guid ContractId { get; set; }
        public Guid ChildId { get; set; }
        public string ChildName { get; set; }
        public Guid PackageId { get; set; }
        public string PackageName { get; set; }
        public Guid MainTutorId { get; set; }
        public string MainTutorName { get; set; }
        public Guid? CenterId { get; set; }
        public string? CenterName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TimeSlot { get; set; }
        public bool IsOnline { get; set; }
        public string Status { get; set; }
    }
}
