using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/contracts")]
    [ApiController]
    [Authorize(Roles = "parent")]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService ?? throw new ArgumentNullException(nameof(contractService));
        }/// <summary>
        /// Create a new contract. Note: DaysOfWeeks uses bitmask format where Sunday=1 (bit 0),
        /// Monday=2 (bit 1), Tuesday=4 (bit 2), etc., up to Saturday=64 (bit 6).
        /// Valid range: 1-127 (at least one day). All days=127.
        /// Common examples: Weekdays Mon-Fri=62, Weekends Sat-Sun=65, Mon Wed Fri=42, Tue Thu=20.
        /// Time range: StartTime after 16:00, EndTime before 22:00, EndTime > StartTime.
        /// </summary>
        [HttpPost]public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var contractId = await _contractService.CreateContractAsync(request);
                return Ok(new { contractId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }/// <summary>
        /// Get contracts by parent ID. Response includes DaysOfWeeksDisplay formatted from bitmask
        /// (e.g., "Mon, Wed, Fri" for value 42). DaysOfWeeks: Sunday=1 (bit 0), Monday=2 (bit 1),
        /// Tuesday=4 (bit 2), Wednesday=8 (bit 3), Thursday=16 (bit 4), Friday=32 (bit 5),
        /// Saturday=64 (bit 6). All days=127. Common: Weekdays Mon-Fri=62, Weekends=65.
        /// </summary>
        /// <param name="parentId">Parent GUID</param>
        /// <returns>List of ContractDto with time and bitmask details</returns>
        [HttpGet("parents/{parentId}")]public async Task<IActionResult> GetContractsByParent(Guid parentId)
        {
            try
            {
                var contracts = await _contractService.GetContractsByParentAsync(parentId);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}