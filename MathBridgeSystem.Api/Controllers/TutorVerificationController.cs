using MathBridgeSystem.Application.DTOs.TutorVerification;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/tutor-verifications")]
    [ApiController]
    [Authorize]
    public class TutorVerificationController : ControllerBase
    {
        private readonly ITutorVerificationService _verificationService;

        public TutorVerificationController(ITutorVerificationService verificationService)
        {
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in claims");
            }
            return userId;
        }

        /// <summary>
        /// Create a new tutor verification
        /// </summary>
        /// <param name="request">Verification creation data</param>
        /// <returns>Created verification ID</returns>
        [HttpPost]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> CreateVerification([FromBody] CreateTutorVerificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = GetCurrentUserId();
                    if (request.UserId != userId)
                        return Forbid("Tutors can only create their own verification");
                }

                var verificationId = await _verificationService.CreateVerificationAsync(request);
                return CreatedAtAction(nameof(GetVerificationById), new { id = verificationId },
                    new { message = "Verification created successfully", verificationId });
            }
            catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
            {
                return Conflict(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing tutor verification
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Success message</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "tutor,admin,staff")]
        public async Task<IActionResult> UpdateVerification(Guid id, [FromBody] UpdateTutorVerificationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var verification = await _verificationService.GetVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Verification not found." });

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == "tutor")
                {
                    var userId = GetCurrentUserId();
                    if (verification.UserId != userId)
                        return Forbid("Tutors can only update their own verification");
                }

                await _verificationService.UpdateVerificationAsync(id, request);
                return Ok(new { message = "Verification updated successfully", verificationId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get verification by ID
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <returns>Verification details</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetVerificationById(Guid id)
        {
            try
            {
                var verification = await _verificationService.GetVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Verification not found." });

                return Ok(verification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get verification by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Verification details</returns>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetVerificationByUserId(Guid userId)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var currentUserId = GetCurrentUserId();

                if (userRole == "tutor" && userId != currentUserId)
                    return Forbid("Tutors can only view their own verification");

                var verification = await _verificationService.GetVerificationByUserIdAsync(userId);
                if (verification == null)
                    return NotFound(new { error = "Verification not found for user." });

                return Ok(verification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all verifications (admin only)
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of verifications with pagination</returns>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllVerifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var verifications = await _verificationService.GetAllVerificationsAsync();
                var totalCount = verifications.Count;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var pagedVerifications = verifications
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return Ok(new
                {
                    data = pagedVerifications,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNext = page < totalPages,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving verifications.", details = ex.Message });
            }
        }

        /// <summary>
        /// Soft delete a verification
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> SoftDeleteVerification(Guid id)
        {
            try
            {
                var verification = await _verificationService.GetVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Verification not found." });

                await _verificationService.SoftDeleteVerificationAsync(id);
                return Ok(new { message = "Verification soft deleted successfully", verificationId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get pending verifications
        /// </summary>
        /// <returns>List of pending verifications</returns>
        [HttpGet("status/pending")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetPendingVerifications()
        {
            try
            {
                var verifications = await _verificationService.GetPendingVerificationsAsync();
                return Ok(new { data = verifications, total = verifications.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving pending verifications.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get approved verifications
        /// </summary>
        /// <returns>List of approved verifications</returns>
        [HttpGet("status/approved")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetApprovedVerifications()
        {
            try
            {
                var verifications = await _verificationService.GetApprovedVerificationsAsync();
                return Ok(new { data = verifications, total = verifications.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving approved verifications.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get rejected verifications
        /// </summary>
        /// <returns>List of rejected verifications</returns>
        [HttpGet("status/rejected")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetRejectedVerifications()
        {
            try
            {
                var verifications = await _verificationService.GetRejectedVerificationsAsync();
                return Ok(new { data = verifications, total = verifications.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving rejected verifications.", details = ex.Message });
            }
        }

        /// <summary>
        /// Approve a tutor verification
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> ApproveVerification(Guid id)
        {
            try
            {
                var verification = await _verificationService.GetVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Verification not found." });

                await _verificationService.ApproveVerificationAsync(id);
                return Ok(new { message = "Verification approved successfully", verificationId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while approving verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Reject a tutor verification
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <param name="reason">Rejection reason</param>
        /// <returns>Success message</returns>
        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> RejectVerification(Guid id)
        {
            try
            {
                var verification = await _verificationService.GetVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Verification not found." });

                await _verificationService.RejectVerificationAsync(id);
                return Ok(new { message = "Verification rejected successfully", verificationId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while rejecting verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get deleted verifications (admin only)
        /// </summary>
        /// <returns>List of soft-deleted verifications</returns>
        [HttpGet("deleted")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetDeletedVerifications()
        {
            try
            {
                var verifications = await _verificationService.GetDeletedVerificationsAsync();
                return Ok(new { data = verifications, total = verifications.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving deleted verifications.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a deleted verification by ID (admin only)
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <returns>Deleted verification details</returns>
        [HttpGet("deleted/{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetDeletedVerificationById(Guid id)
        {
            try
            {
                var verification = await _verificationService.GetDeletedVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Deleted verification not found." });

                return Ok(verification);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving deleted verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Restore a soft-deleted verification (admin only)
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <returns>Success message</returns>
        [HttpPatch("restore/{id}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> RestoreVerification(Guid id)
        {
            try
            {
                var verification = await _verificationService.GetDeletedVerificationByIdAsync(id);
                if (verification == null)
                    return NotFound(new { error = "Deleted verification not found." });

                await _verificationService.RestoreVerificationAsync(id);
                return Ok(new { message = "Verification restored successfully", verificationId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while restoring verification.", details = ex.Message });
            }
        }

        /// <summary>
        /// Permanently delete a verification (admin only)
        /// </summary>
        /// <param name="id">Verification ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("permanent/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PermanentlyDeleteVerification(Guid id)
        {
            try
            {
                await _verificationService.PermanentlyDeleteVerificationAsync(id);
                return Ok(new { message = "Verification permanently deleted", verificationId = id });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while permanently deleting verification.", details = ex.Message });
            }
        }
    }
}
