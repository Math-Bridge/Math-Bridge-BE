using MathBridgeSystem.Application.DTOs.SessionUnitAssignment;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    /// <summary>
    /// Controller for session unit assignment operations
    /// </summary>
    [Route("api/session-unit-assignment")]
    [ApiController]
    public class SessionUnitAssignmentController : ControllerBase
    {
        private readonly ISessionUnitAssignmentService _sessionUnitAssignmentService;

        public SessionUnitAssignmentController(ISessionUnitAssignmentService sessionUnitAssignmentService)
        {
            _sessionUnitAssignmentService = sessionUnitAssignmentService 
                ?? throw new ArgumentNullException(nameof(sessionUnitAssignmentService));
        }

        /// <summary>
        /// Assigns units to all sessions of a contract based on daily reports
        /// </summary>
        /// <param name="request">Request containing the contract ID</param>
        /// <returns>Assignment result with details</returns>
        /// <remarks>
        /// If no daily reports exist, assigns from the first unit until sessions run out.
        /// If daily reports exist, starts from the unit of the oldest daily report.
        /// Sessions without available units will have UnitId set to null.
        /// </remarks>
        [HttpPost("assign")]
        [Authorize(Roles = "admin,staff,tutor")]
        public async Task<IActionResult> AssignUnitsToContractSessions([FromBody] AssignUnitsToContractSessionsRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _sessionUnitAssignmentService.AssignUnitsToContractSessionsAsync(request.ContractId);
                return Ok(new
                {
                    message = "Units assigned to sessions successfully",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while assigning units to sessions." });
            }
        }

        /// <summary>
        /// Assigns units to all sessions of a contract by contract ID (alternative endpoint)
        /// </summary>
        /// <param name="contractId">The contract ID</param>
        /// <returns>Assignment result with details</returns>
        [HttpPost("assign/{contractId}")]
        [Authorize(Roles = "admin,staff,tutor")]
        public async Task<IActionResult> AssignUnitsToContractSessionsById(Guid contractId)
        {
            try
            {
                var result = await _sessionUnitAssignmentService.AssignUnitsToContractSessionsAsync(contractId);
                return Ok(new
                {
                    message = "Units assigned to sessions successfully",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while assigning units to sessions." });
            }
        }
    }
}
