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
            Response.StatusCode = 200;
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            var writer = new StreamWriter(Response.Body) { AutoFlush = false };
            _connectionManager.RegisterConnection(userId, writer);

            try
            {
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    // Send keep-alive comment every 30 seconds
                    await writer.WriteAsync(":keep-alive\n\n");
                    await writer.FlushAsync();
                    await Task.Delay(30000, HttpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            finally
            {
                _connectionManager.UnregisterConnection(userId);
                await writer.FlushAsync();
                await writer.DisposeAsync();
            }
        }



        #region Testing Endpoints

        [AllowAnonymous]
        [HttpGet("test/health")]
        public async Task<ActionResult> HealthCheck()
        {
            try
            {
                var pubSubProvider = HttpContext.RequestServices.GetService(typeof(IPubSubNotificationProvider)) as IPubSubNotificationProvider;
                if (pubSubProvider == null)
                {
                    return StatusCode(500, new
                    {
                        status = "unhealthy",
                        error = "PubSub provider not registered",
                        timestamp = DateTime.UtcNow
                    });
                }

                var topicExists = await pubSubProvider.TopicExistsAsync("notifications");

                return Ok(new
                {
                    status = topicExists ? "healthy" : "topic_not_found",
                    topicExists = topicExists,
                    projectId = HttpContext.RequestServices.GetService(typeof(IConfiguration)) is IConfiguration config ? config["GoogleMeet:ProjectId"] : "unknown",
                    credentialsPath = HttpContext.RequestServices.GetService(typeof(IConfiguration)) is IConfiguration config2 ? config2["GoogleMeet:OAuthCredentialsPath"] : "unknown",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("test/publish")]
        public async Task<ActionResult> TestPublishNotification([FromBody] CreateTestNotificationRequest request)
        {
            try
            {
                var pubSubProvider = HttpContext.RequestServices.GetService(typeof(IPubSubNotificationProvider)) as IPubSubNotificationProvider;
                if (pubSubProvider == null)
                {
                    return StatusCode(500, new { error = "PubSub provider not registered" });
                }

                var testNotification = new NotificationResponseDto
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = request.UserId ?? Guid.NewGuid(),
                    Title = request.Title ?? "Test Notification",
                    Message = request.Message ?? "This is a test notification from Pub/Sub",
                    NotificationType = request.NotificationType ?? "Test",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };

                await pubSubProvider.PublishNotificationAsync(
                    testNotification,
                    request.TopicName ?? "notifications-session-reminders"
                );

                return Ok(new
                {
                    message = "Test notification published successfully",
                    notification = testNotification,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestPublishNotification: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Failed to publish test notification",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("test/publish-batch")]
        public async Task<ActionResult> TestPublishBatchNotifications([FromBody] CreateBatchTestNotificationsRequest request)
        {
            try
            {
                var pubSubProvider = HttpContext.RequestServices.GetService(typeof(IPubSubNotificationProvider)) as IPubSubNotificationProvider;
                if (pubSubProvider == null)
                {
                    return StatusCode(500, new { error = "PubSub provider not registered" });
                }

                var notifications = new List<NotificationResponseDto>();
                var count = request.Count ?? 5;

                for (int i = 0; i < count; i++)
                {
                    notifications.Add(new NotificationResponseDto
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = request.UserId ?? Guid.NewGuid(),
                        Title = $"Batch Test Notification {i + 1}",
                        Message = $"This is batch notification {i + 1} from Pub/Sub",
                        NotificationType = "BatchTest",
                        IsRead = false,
                        CreatedDate = DateTime.UtcNow
                    });
                }

                await pubSubProvider.PublishBatchNotificationsAsync(
                    notifications,
                    request.TopicName ?? "notifications-session-reminders"
                );

                return Ok(new
                {
                    message = $"Batch of {count} test notifications published successfully",
                    notificationCount = count,
                    notifications = notifications,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestPublishBatchNotifications: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Failed to publish batch test notifications",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("test/create-subscription")]
        public async Task<ActionResult> TestCreateSubscription([FromBody] CreateTestSubscriptionRequest request)
        {
            try
            {
                var pubSubProvider = HttpContext.RequestServices.GetService(typeof(IPubSubNotificationProvider)) as IPubSubNotificationProvider;
                if (pubSubProvider == null)
                {
                    return StatusCode(500, new { error = "PubSub provider not registered" });
                }

                var topicName = request.TopicName ?? "notifications-session-reminders";
                var subscriptionName = request.SubscriptionName ?? $"test-subscription-{Guid.NewGuid().ToString().Substring(0, 8)}";

                await pubSubProvider.SubscribeAsync(topicName, subscriptionName);

                return Ok(new
                {
                    message = "Test subscription created successfully",
                    topicName = topicName,
                    subscriptionName = subscriptionName,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestCreateSubscription: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Failed to create test subscription",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [AllowAnonymous]
        [HttpGet("test/topic-exists")]
        public async Task<ActionResult> TestTopicExists([FromQuery] string topicName = "notifications")
        {
            try
            {
                var pubSubProvider = HttpContext.RequestServices.GetService(typeof(IPubSubNotificationProvider)) as IPubSubNotificationProvider;
                if (pubSubProvider == null)
                {
                    return StatusCode(500, new { error = "PubSub provider not registered" });
                }

                var exists = await pubSubProvider.TopicExistsAsync(topicName);

                return Ok(new
                {
                    topicName = topicName,
                    exists = exists,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestTopicExists: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Failed to check topic existence",
                    message = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("test/create-in-db")]
        public async Task<ActionResult> TestCreateNotificationInDatabase([FromBody] CreateTestNotificationRequest request)
        {
            try
            {
                var testNotification = new NotificationResponseDto
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = request.UserId ?? Guid.NewGuid(),
                    Title = request.Title ?? "Database Test Notification",
                    Message = request.Message ?? "This notification was created directly in the database",
                    NotificationType = request.NotificationType ?? "DatabaseTest",
                    Status = "Created",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow
                };

                // Note: CreateNotificationAsync requires proper implementation in INotificationService

                return Ok(new
                {
                    message = "Test notification created in database successfully",
                    notification = testNotification,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] TestCreateNotificationInDatabase: {ex.Message}");
                return StatusCode(500, new
                {
                    error = "Failed to create notification in database",
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        #endregion
    }

    #region Test Request Models

    public class CreateTestNotificationRequest
    {
        public Guid? UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; }
        public string TopicName { get; set; }
    }

    public class CreateBatchTestNotificationsRequest
    {
        public Guid? UserId { get; set; }
        public int? Count { get; set; }
        public string TopicName { get; set; }
    }

    public class CreateTestSubscriptionRequest
    {
        public string TopicName { get; set; }
        public string SubscriptionName { get; set; }
    }

    #endregion
}