using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs
{
    public class AvailableSubTutorsDto
    {
        public List<SubTutorInfoDto> AvailableTutors { get; set; } = new List<SubTutorInfoDto>();
        public int TotalAvailable { get; set; }
    }

    public class SubTutorInfoDto
    {
        public Guid TutorId { get; set; }
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public double? Rating { get; set; }
        public bool IsAvailable { get; set; }
    }
}

