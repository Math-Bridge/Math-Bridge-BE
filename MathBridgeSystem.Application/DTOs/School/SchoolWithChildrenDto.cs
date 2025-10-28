using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.School
{
    public class SchoolWithChildrenDto : SchoolDto
    {
        public List<ChildDto> Children { get; set; } = new List<ChildDto>();
    }
}