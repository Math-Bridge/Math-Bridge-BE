using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class CreateContractRequest
    {
        public Guid ParentId { get; set; }
        public Guid ChildId { get; set; }
        public Guid PackageId { get; set; }
        public Guid MainTutorId { get; set; }
        public Guid? SubstituteTutor1Id { get; set; }
        public Guid? SubstituteTutor2Id { get; set; }
        public Guid? CenterId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string TimeSlot { get; set; }
        public bool IsOnline { get; set; }
        public string? OfflineAddress { get; set; }
        public decimal? OfflineLatitude { get; set; }
        public decimal? OfflineLongitude { get; set; }
        public string? VideoCallPlatform { get; set; }
        public decimal MaxDistanceKm { get; set; }
    }
}
