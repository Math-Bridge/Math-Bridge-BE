using MathBridgeSystem.Application.DTOs.Withdrawal;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/withdrawals")]
    [ApiController]
    public class WithdrawalController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;

        public WithdrawalController(IWithdrawalService withdrawalService)
        {
            _withdrawalService = withdrawalService;
        }

        private Guid GetUserId()
        {
             var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
             if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                 throw new UnauthorizedAccessException("Invalid User ID");
             return userId;
        }

        [HttpPost("request")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> RequestWithdrawal([FromBody] WithdrawalRequestCreateDto requestDto)
        {
            try
            {
                var userId = GetUserId();
                var request = await _withdrawalService.RequestWithdrawalAsync(userId, requestDto);
                return Ok(request);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("process/{id}")]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> ProcessWithdrawal(Guid id)
        {
            try
            {
                var staffId = GetUserId();
                var request = await _withdrawalService.ProcessWithdrawalAsync(id, staffId);
                return Ok(request);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var requests = await _withdrawalService.GetPendingRequestsAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("my-requests")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetMyRequests()
        {
            try
            {
                var userId = GetUserId();
                var requests = await _withdrawalService.GetMyRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}