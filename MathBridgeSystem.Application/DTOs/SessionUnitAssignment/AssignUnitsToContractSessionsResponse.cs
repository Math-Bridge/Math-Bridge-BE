using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Application.DTOs.SessionUnitAssignment
{
    /// <summary>
    /// Response DTO for unit assignment result
    /// </summary>
    public class AssignUnitsToContractSessionsResponse
    {
        /// <summary>
        /// The contract ID that was processed
        /// </summary>
        public Guid ContractId { get; set; }

        /// <summary>
        /// Total sessions found for the contract
        /// </summary>
        public int TotalSessions { get; set; }

        /// <summary>
        /// Number of sessions that were assigned a unit
        /// </summary>
        public int SessionsAssigned { get; set; }

        /// <summary>
        /// Number of sessions left without a unit (when units ran out)
        /// </summary>
        public int SessionsWithoutUnit { get; set; }

        /// <summary>
        /// The starting unit order used for assignment
        /// </summary>
        public int StartingUnitOrder { get; set; }

        /// <summary>
        /// Details of each session assignment
        /// </summary>
        public List<SessionUnitAssignmentDetail> AssignmentDetails { get; set; } = new();
    }

    /// <summary>
    /// Detail of a single session unit assignment
    /// </summary>
    public class SessionUnitAssignmentDetail
    {
        /// <summary>
        /// Session booking ID
        /// </summary>
        public Guid BookingId { get; set; }

        /// <summary>
        /// Session date
        /// </summary>
        public DateOnly SessionDate { get; set; }

        /// <summary>
        /// Assigned unit ID (null if no unit available)
        /// </summary>
        public Guid? UnitId { get; set; }

        /// <summary>
        /// Assigned unit name (null if no unit available)
        /// </summary>
        public string? UnitName { get; set; }

        /// <summary>
        /// Unit order in curriculum
        /// </summary>
        public int? UnitOrder { get; set; }
    }
}
