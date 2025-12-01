using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class ReplaceMainTutorRequest
    {
        public Guid NewMainTutorId { get; set; }
        public Guid NewSubstituteTutorId { get; set; }
    }
}
