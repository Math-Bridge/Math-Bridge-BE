using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.DTOs
{
    public class MathProgramDto
    {
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string? Description { get; set; }
    }
}
