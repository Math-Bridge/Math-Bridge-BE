using MathBridgeSystem.Application.DTOs.WalletTransaction;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/wallet-transactions")]
    [ApiController]
    public class WalletTransactionController : ControllerBase
    {
        private readonly IWalletTransactionService _transactionService;

        public WalletTransactionController(IWalletTransactionService transactionService)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        }

        private Guid GetUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new UnauthorizedAccessException("Missing or invalid user ID in token.");
            return userId;
        }

        /// <summary>
        /// Get a transaction by ID
        /// </summary>
        [HttpGet("{transactionId}")]
        [Authorize(Roles = "parent,staff,admin")]
        public async Task<IActionResult> GetTransactionById(Guid transactionId)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
                
                // Parents can only view their own transactions
                if (User.IsInRole("parent"))
                {
                    var userId = GetUserId();
                    if (transaction.ParentId != userId)
                        return Forbid("You can only view your own transactions.");
                }

                return Ok(transaction);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all transactions for a parent
        /// </summary>
        [HttpGet("parent/{parentId}")]
        [Authorize(Roles = "parent,staff,admin")]
        public async Task<IActionResult> GetTransactionsByParentId(Guid parentId)
        {
            try
            {
                // Parents can only view their own transactions
                if (User.IsInRole("parent"))
                {
                    var userId = GetUserId();
                    if (parentId != userId)
                        return Forbid("You can only view your own transactions.");
                }

                var transactions = await _transactionService.GetTransactionsByParentIdAsync(parentId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's transactions
        /// </summary>
        [HttpGet("my-transactions")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetMyTransactions()
        {
            try
            {
                var userId = GetUserId();
                var transactions = await _transactionService.GetTransactionsByParentIdAsync(userId);
                return Ok(transactions);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get wallet balance for a parent
        /// </summary>
        [HttpGet("parent/{parentId}/balance")]
        [Authorize(Roles = "parent,staff,admin")]
        public async Task<IActionResult> GetWalletBalance(Guid parentId)
        {
            try
            {
                // Parents can only view their own balance
                if (User.IsInRole("parent"))
                {
                    var userId = GetUserId();
                    if (parentId != userId)
                        return Forbid("You can only view your own wallet balance.");
                }

                var balance = await _transactionService.GetParentWalletBalanceAsync(parentId);
                return Ok(new { parentId, balance });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's wallet balance
        /// </summary>
        [HttpGet("my-balance")]
        [Authorize(Roles = "parent")]
        public async Task<IActionResult> GetMyBalance()
        {
            try
            {
                var userId = GetUserId();
                var balance = await _transactionService.GetParentWalletBalanceAsync(userId);
                return Ok(new { balance });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new transaction
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateWalletTransactionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var transactionId = await _transactionService.CreateTransactionAsync(request);
                return CreatedAtAction(nameof(GetTransactionById), new { transactionId },
                    new { message = "Transaction created successfully", transactionId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the transaction.", details = ex.Message });
            }
        }

        /// <summary>
        /// Update transaction status
        /// </summary>
        [HttpPut("{transactionId}/status")]
        [Authorize(Roles = "staff,admin")]
        public async Task<IActionResult> UpdateTransactionStatus(Guid transactionId, [FromBody] UpdateTransactionStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _transactionService.UpdateTransactionStatusAsync(transactionId, request.Status);
                return Ok(new { message = "Transaction status updated successfully", transactionId });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the transaction.", details = ex.Message });
            }
        }
    }
}