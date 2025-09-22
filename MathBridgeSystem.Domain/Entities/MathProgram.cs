using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Domain.Entities
{
    public partial class MathProgram
    {
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; } = null!;
        public string? Description { get; set; }
        public virtual ICollection<PaymentPackage> PaymentPackages { get; set; } = new List<PaymentPackage>();
    }
}
