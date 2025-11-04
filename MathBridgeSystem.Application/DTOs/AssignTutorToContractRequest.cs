using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class AssignTutorToContractRequest
    {
        [JsonPropertyName("mainTutorId")]
        public Guid MainTutorId { get; set; }

        [JsonPropertyName("substituteTutor1Id")]
        public Guid? SubstituteTutor1Id { get; set; }

        [JsonPropertyName("substituteTutor2Id")]
        public Guid? SubstituteTutor2Id { get; set; }
    }
}
