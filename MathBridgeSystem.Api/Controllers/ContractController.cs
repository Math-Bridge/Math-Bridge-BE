// MathBridgeSystem.Api.Controllers/ContractController.cs
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
        }

        [HttpPost]
        public async Task<IActionResult> CreateContract([FromBody] CreateContractRequest request)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var contractId = await _contractService.CreateContractAsync(request);
                return Ok(new { contractId, message = "Contract created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{contractId}/status")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> UpdateStatus(Guid contractId, [FromBody] UpdateContractStatusRequest request)
        {
            var staffId = Guid.Parse(User.FindFirst("sub")?.Value!);
            try
            {
                await _contractService.UpdateContractStatusAsync(contractId, request, staffId);
                return Ok(new { success = true, message = $"Status updated to: {request.Status}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("parents/{parentId}")]
        public async Task<IActionResult> GetContractsByParent(Guid parentId)
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

        [HttpPut("{contractId}/assign-tutors")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> AssignTutors(Guid contractId, [FromBody] MathBridgeSystem.Application.DTOs.AssignTutorToContractRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            var staffId = Guid.Parse(User.FindFirst("sub")?.Value!);
            try
            {
                object value = await _contractService.AssignTutorsAsync(contractId, request, staffId);
                return Ok(new { success = true, message = "Đã gán tutor và tạo buổi học." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}