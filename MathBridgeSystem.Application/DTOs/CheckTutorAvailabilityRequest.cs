using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class CheckTutorAvailabilityRequest
    {
        [Required] public Guid PackageId { get; set; }
        [Required] public Guid ChildId { get; set; }
        public Guid? SecondChildId { get; set; }
        [Required] public DateOnly StartDate { get; set; }
        [Required] public DateOnly EndDate { get; set; }
        [Required] public List<ContractScheduleDto> Schedules { get; set; } = new();
        [Required] public bool IsOnline { get; set; }
        public string? OfflineAddress { get; set; }
        public decimal? OfflineLatitude { get; set; }
        public decimal? OfflineLongitude { get; set; }
        public decimal? MaxDistanceKm { get; set; } = 15;
    }
}
