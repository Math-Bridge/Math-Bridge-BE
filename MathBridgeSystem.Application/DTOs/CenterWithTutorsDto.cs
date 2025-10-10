using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class CenterWithTutorsDto
    {
        public Guid CenterId { get; set; }
        public string Name { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? FormattedAddress { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? PlaceName { get; set; }
        public string? CountryCode { get; set; }
        public int TutorCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<TutorInCenterDto> Tutors { get; set; } = new List<TutorInCenterDto>();
    }
}