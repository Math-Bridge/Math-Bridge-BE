using MathBridgeSystem.Application.DTOs.NotificationLog;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationLogController : ControllerBase
    {
        private readonly INotificationLogService _notificationLogService;

        public NotificationLogController(INotificationLogService notificationLogService)
        {
            _notificationLogService = notificationLogService ?? throw new ArgumentNullException(nameof(notificationLogService));
        }

        /// <summary>
        /// Get notification log by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationLogDto>> GetById(Guid id)
        {
            var log = await _notificationLogService.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound(new { message = "Notification log not found" });
            }
            return Ok(log);
        }

        /// <summary>
        /// Get all notification logs
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetAll()
        {
            var logs = await _notificationLogService.GetAllAsync();
            return Ok(logs);
        }

        /// <summary>
        /// Get logs by notification ID
        /// </summary>
        [HttpGet("notification/{notificationId}")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetByNotificationId(Guid notificationId)
        {
            var logs = await _notificationLogService.GetByNotificationIdAsync(notificationId);
            return Ok(logs);
        }

        /// <summary>
        /// Get logs by contract ID
        /// </summary>
        [HttpGet("contract/{contractId}")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetByContractId(Guid contractId)
        {
            var logs = await _notificationLogService.GetByContractIdAsync(contractId);
            return Ok(logs);
        }

        /// <summary>
        /// Get logs by session ID
        /// </summary>
        [HttpGet("session/{sessionId}")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetBySessionId(Guid sessionId)
        {
            var logs = await _notificationLogService.GetBySessionIdAsync(sessionId);
            return Ok(logs);
        }

        /// <summary>
        /// Get logs by channel
        /// </summary>
        [HttpGet("channel/{channel}")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetByChannel(string channel)
        {
            var logs = await _notificationLogService.GetByChannelAsync(channel);
            return Ok(logs);
        }

        /// <summary>
        /// Get logs by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetByStatus(string status)
        {
            var logs = await _notificationLogService.GetByStatusAsync(status);
            return Ok(logs);
        }

        /// <summary>
        /// Get failed notification logs
        /// </summary>
        [HttpGet("failed")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<NotificationLogDto>>> GetFailedLogs()
        {
            var logs = await _notificationLogService.GetFailedLogsAsync();
            return Ok(logs);
        }

        /// <summary>
        /// Search notification logs with filters
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<List<NotificationLogDto>>> SearchLogs([FromBody] NotificationLogSearchRequest request)
        {
            var logs = await _notificationLogService.SearchLogsAsync(request);
            return Ok(logs);
        }

        /// <summary>
        /// Create a new notification log
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<NotificationLogDto>> Create([FromBody] CreateNotificationLogRequest request)
        {
            try
            {
                var log = await _notificationLogService.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = log.LogId }, log);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to create notification log", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a notification log
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _notificationLogService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Notification log not found" });
            }
            return Ok(new { message = "Notification log deleted successfully" });
        }
    }
}