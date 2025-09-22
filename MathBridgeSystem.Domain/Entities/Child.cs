using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Domain.Entities
{
    public partial class Child
    {
        public Guid ChildId { get; set; }
        public Guid ParentId { get; set; }
        public string FullName { get; set; } = null!;
        public Guid SchoolId { get; set; }
        public Guid? CenterId { get; set; }
        public string Grade { get; set; } = null!;
        public DateTime? DateOfBirth { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "active"; // Added for soft delete: 'active' or 'deleted'

        public virtual User Parent { get; set; } = null!;
        public virtual School School { get; set; } = null!;
        public virtual Center? Center { get; set; }
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
