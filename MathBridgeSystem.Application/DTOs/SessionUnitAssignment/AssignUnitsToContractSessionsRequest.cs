using System;

namespace MathBridgeSystem.Application.DTOs.SessionUnitAssignment
{
    /// <summary>
    /// Request DTO for assigning units to all sessions of a contract
    /// </summary>
    public class AssignUnitsToContractSessionsRequest
    {
        /// <summary>
        /// The contract ID to assign units to
        /// </summary>
        public Guid ContractId { get; set; }
    }
}
