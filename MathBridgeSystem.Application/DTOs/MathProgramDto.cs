using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class MathProgramDto
    {
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; } = null!;
        public string? Description { get; set; }
        public string? LinkSyllabus { get; set; }
        public int PackageCount { get; set; }
        public int TestResultCount { get; set; }
    }

    public class CreateMathProgramRequest
    {
        public string ProgramName { get; set; } = null!;
        public string? Description { get; set; }
        public string? LinkSyllabus { get; set; }
    }

    public class UpdateMathProgramRequest
    {
        public string ProgramName { get; set; } = null!;
        public string? Description { get; set; }
        public string? LinkSyllabus { get; set; }
    }
}
