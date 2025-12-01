using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class ChildDto
    {
        public Guid ChildId { get; set; }
        public string FullName { get; set; }
        public Guid SchoolId { get; set; }
        public string SchoolName { get; set; }
        public Guid? CenterId { get; set; }
        public string? CenterName { get; set; }
        public string Grade { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Status { get; set; }
        public string? AvatarUrl { get; set; }
        public byte? AvatarVersion { get; set; }
    }
}
