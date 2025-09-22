using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Domain.Entities
{
    public partial class TutorCenter
    {
        public Guid TutorCenterId { get; set; }
        public Guid TutorId { get; set; }
        public Guid CenterId { get; set; }
        public DateTime CreatedDate { get; set; }

        [ForeignKey("TutorId")]
        public virtual User Tutor { get; set; } = null!;

        [ForeignKey("CenterId")]
        public virtual Center Center { get; set; } = null!;
    }
}
