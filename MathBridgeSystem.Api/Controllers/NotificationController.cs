using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
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
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly NotificationConnectionManager _connectionManager;

        public NotificationController(
            INotificationService notificationService,
            NotificationConnectionManager connectionManager)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
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

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationResponseDto>>> GetNotifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId, pageNumber, pageSize);
            return Ok(notifications);
        }

        [HttpGet("unread")]
        public async Task<ActionResult<List<NotificationResponseDto>>> GetUnreadNotifications()
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUnreadNotificationsByUserIdAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationResponseDto>> GetNotification(Guid id)
        {
            var notification = await _notificationService.GetNotificationByIdAsync(id);
            if (notification == null)
            {
                return NotFound(new { message = "Notification not found" });
            }
            return Ok(notification);
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(id);
            return Ok(new { message = "Notification marked as read" });
        }

        [HttpPut("mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(Guid id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.DeleteNotificationAsync(id);
            return Ok(new { message = "Notification deleted" });
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteAllNotifications()
        {
            var userId = GetCurrentUserId();
            await _notificationService.DeleteAllNotificationsAsync(userId);
            return Ok(new { message = "All notifications deleted" });
        }

        [HttpGet("sse/connect")]
        public async Task SubscribeToNotifications()
        {
            var userId = GetCurrentUserId();
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            Response.Headers.Add("X-Accel-Buffering", "no");

            var writer = new System.IO.StreamWriter(Response.Body);
            _connectionManager.RegisterConnection(userId, writer);

            try
            {
                // Keep connection open
                await Task.Delay(Timeout.Infinite, HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            finally
            {
                _connectionManager.UnregisterConnection(userId);
                await writer.FlushAsync();
                writer.Dispose();
            }
        }
    }
}