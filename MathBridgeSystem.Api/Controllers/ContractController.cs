using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/contracts")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;

        public ContractController(IContractService contractService)
        {
            _contractService = contractService ?? throw new ArgumentNullException(nameof(contractService));
        }

        private Guid GetUserIdFromClaims()
        {
            var sub = User.FindFirst("sub")?.Value ??
                      User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                      User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(sub))
                throw new UnauthorizedAccessException("Missing user identifier in token.");

            if (!Guid.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Invalid user identifier format.");

            return userId;
        }

        [HttpPost]
        [Authorize(Roles = "parent")]
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
            var staffId = GetUserIdFromClaims(); 
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
        [Authorize(Roles = "parent,staff")]
        public async Task<IActionResult> GetContractsByParent(Guid parentId)
        {
            var userId = GetUserIdFromClaims();
            if (parentId != userId)
                return Forbid();

            try
            {
                var contracts = await _contractService.GetContractsByParentAsync(parentId);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetContractsByParent: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve contracts.", details = ex.Message });
            }
        }

        [HttpPut("{contractId}/assign-tutors")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> AssignTutors(Guid contractId, [FromBody] MathBridgeSystem.Application.DTOs.AssignTutorToContractRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            var staffId = GetUserIdFromClaims(); 
            try
            {
                await _contractService.AssignTutorsAsync(contractId, request, staffId);
                return Ok(new { success = true, message = "Đã gán tutor và tạo buổi học." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all contracts (Staff & Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetAllContracts()
        {
            try
            {
                var contracts = await _contractService.GetAllContractsAsync();
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve all contracts.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get contract by ID (Staff & Admin only)
        /// </summary>
        [HttpGet("{contractId}")]
        [Authorize(Roles = "admin,staff,parent,tutor")]
        public async Task<IActionResult> GetContractById(Guid contractId)
        {
            try
            {
                var contract = await _contractService.GetContractByIdAsync(contractId);
                return Ok(contract);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Contract not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve contract.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all contracts linked to parent phone number (Staff & Admin only)
        /// </summary>
        [HttpGet("by-phone/{phoneNumber}")]
        [Authorize(Roles = "admin,staff")]
        public async Task<IActionResult> GetContractsByParentPhone(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return BadRequest(new { error = "Phone number cannot be empty." });

                var contracts = await _contractService.GetContractsByParentPhoneAsync(phoneNumber);
                
                if (contracts == null || contracts.Count == 0)
                    return NotFound(new { message = "No contracts found for this phone number." });

                return Ok(contracts);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetContractsByParentPhone: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve contracts.", details = ex.Message });
            }
        }

        /// <summary>
        /// Staff completes a contract (all sessions must be done)
        /// </summary>
        [HttpPut("{contractId}/complete")]
        [Authorize(Roles = "staff")]
        public async Task<IActionResult> CompleteContract(Guid contractId)
        {
            var staffId = GetUserIdFromClaims();
            try
            {
                var success = await _contractService.CompleteContractAsync(contractId, staffId);
                return Ok(new { success, message = "Contract completed successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Contract not found." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get available tutors for a contract (no overlapping contracts)
        /// </summary>
        /// <param name="contractId">The contract ID</param>
        /// <param name="sortByRating">Enable sorting by highest rating (default: false)</param>
        /// <param name="sortByDistance">Enable sorting by nearest distance for offline contracts (default: false)</param>
        /// <returns>List of available tutors with rating and distance information</returns>
        [HttpGet("{contractId}/available-tutors")]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> GetAvailableTutors(
            Guid contractId,
            [FromQuery] bool sortByRating = false,
            [FromQuery] bool sortByDistance = false)
        {
            try
            {
                var availableTutors = await _contractService.GetAvailableTutorsAsync(contractId, sortByRating, sortByDistance);

                if (availableTutors == null || availableTutors.Count == 0)
                    return Ok(new List<object>());

                return Ok(availableTutors);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAvailableTutors: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve available tutors.", details = ex.Message });
            }
        }

        /// <summary>
        /// Check available tutors BEFORE creating a contract
        /// Only returns matching tutors — no data is saved
        /// </summary>
        [HttpPost("check-available-tutors")]
        [Authorize(Roles = "parent,staff,admin")]
        public async Task<IActionResult> CheckAvailableTutorsBeforeCreate([FromBody] CheckTutorAvailabilityRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            try
            {
                var tutors = await _contractService.CheckTutorsAvailabilityBeforeCreateAsync(request);

                return Ok(new
                {
                    success = true,
                    message = tutors.Count > 0
                        ? $"Found {tutors.Count} tutor(s) available for your schedule."
                        : "No tutor matches your requested schedule and center at the moment.",
                    data = new
                    {
                        totalAvailable = tutors.Count,
                        tutors = tutors
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "System error", details = ex.Message });
            }
        }
    }
}