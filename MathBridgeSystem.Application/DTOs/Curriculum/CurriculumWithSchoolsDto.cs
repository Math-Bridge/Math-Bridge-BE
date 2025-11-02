using System.Collections.Generic;
using MathBridgeSystem.Application.DTOs.School;

namespace MathBridgeSystem.Application.DTOs.Curriculum
{
    public class CurriculumWithSchoolsDto : CurriculumDto
    {
        public List<SchoolDto> Schools { get; set; } = new List<SchoolDto>();
    }
}