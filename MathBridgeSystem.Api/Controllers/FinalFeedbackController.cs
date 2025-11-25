using MathBridgeSystem.Application.DTOs.FinalFeedback;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinalFeedbackController : ControllerBase
    {
        private readonly IFinalFeedbackService _feedbackService;

        public FinalFeedbackController(IFinalFeedbackService feedbackService)
        {
            _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
        }

        /// <summary>
        /// Get feedback by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<FinalFeedbackDto>> GetById(Guid id)
        {
            var feedback = await _feedbackService.GetByIdAsync(id);
            if (feedback == null)
            {
                return NotFound(new { message = "Final feedback not found" });
            }
            return Ok(feedback);
        }

        /// <summary>
        /// Get all feedbacks
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<FinalFeedbackDto>>> GetAll()
        {
            var feedbacks = await _feedbackService.GetAllAsync();
            return Ok(feedbacks);
        }

        /// <summary>
        /// Get feedbacks by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<FinalFeedbackDto>>> GetByUserId(Guid userId)
        {
            var feedbacks = await _feedbackService.GetByUserIdAsync(userId);
            return Ok(feedbacks);
        }

        /// <summary>
        /// Get feedbacks by contract ID
        /// </summary>
        [HttpGet("contract/{contractId}")]
        public async Task<ActionResult<List<FinalFeedbackDto>>> GetByContractId(Guid contractId)
        {
            var feedbacks = await _feedbackService.GetByContractIdAsync(contractId);
            return Ok(feedbacks);
        }

        /// <summary>
        /// Get feedback by contract and provider type
        /// </summary>
        [HttpGet("contract/{contractId}/provider/{providerType}")]
        public async Task<ActionResult<FinalFeedbackDto>> GetByContractAndProviderType(Guid contractId, string providerType)
        {
            var feedback = await _feedbackService.GetByContractAndProviderTypeAsync(contractId, providerType);
            if (feedback == null)
            {
                return NotFound(new { message = "Final feedback not found" });
            }
            return Ok(feedback);
        }

        /// <summary>
        /// Get feedbacks by provider type
        /// </summary>
        [HttpGet("provider/{providerType}")]
        public async Task<ActionResult<List<FinalFeedbackDto>>> GetByProviderType(string providerType)
        {
            var feedbacks = await _feedbackService.GetByProviderTypeAsync(providerType);
            return Ok(feedbacks);
        }

        /// <summary>
        /// Get feedbacks by status
        /// </summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<FinalFeedbackDto>>> GetByStatus(string status)
        {
            var feedbacks = await _feedbackService.GetByStatusAsync(status);
            return Ok(feedbacks);
        }

        [HttpPost]
        [Authorize(Roles = "parent")]
        public async Task<ActionResult<FinalFeedbackDto>> Create([FromBody] CreateFinalFeedbackRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var parentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(parentIdClaim) || !Guid.TryParse(parentIdClaim, out var parentId))
            {
                return Unauthorized("Unable to authenticate user.");
            }

            try
            {
                var feedback = await _feedbackService.CreateAsync(request, parentId);
                return CreatedAtAction(nameof(GetById), new { id = feedback.FeedbackId }, feedback);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "System error while submitting review", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a feedback
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<FinalFeedbackDto>> Update(Guid id, [FromBody] UpdateFinalFeedbackRequest request)
        {
            try
            {
                var feedback = await _feedbackService.UpdateAsync(id, request);
                if (feedback == null)
                {
                    return NotFound(new { message = "Final feedback not found" });
                }
                return Ok(feedback);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to update final feedback", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a feedback
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _feedbackService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Final feedback not found" });
            }
            return Ok(new { message = "Final feedback deleted successfully" });
        }
    }
}