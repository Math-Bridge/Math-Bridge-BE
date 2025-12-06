using MathBridgeSystem.Application.DTOs.SessionUnitAssignment;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    /// <summary>
    /// Service interface for assigning units to contract sessions
    /// </summary>
    public interface ISessionUnitAssignmentService
    {
        /// <summary>
        /// Assigns units to all sessions of a contract based on daily reports
        /// </summary>
        /// <param name="contractId">The contract ID</param>
        /// <returns>Assignment result with details</returns>
        Task<AssignUnitsToContractSessionsResponse> AssignUnitsToContractSessionsAsync(Guid contractId);
    }
}
