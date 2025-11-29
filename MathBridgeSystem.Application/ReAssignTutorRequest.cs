using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs
{
    public class ReAssignTutorRequest
    {
        [Required] public Guid BannedTutorId { get; set; }        
        [Required] public Guid ReplacementTutorId { get; set; }     
        public bool ApplyToAllUpcomingSessions { get; set; } = true; 
    }
}