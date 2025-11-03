using Google.Cloud.PubSub.V1;
using MathBridgeSystem.Application.DTOs.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Services
{
    public class PubSubSubscriberService
    {
        private readonly NotificationConnectionManager _connectionManager;
        private readonly string _projectId;
        private readonly ILogger<PubSubSubscriberService> _logger;

        public PubSubSubscriberService(
            NotificationConnectionManager connectionManager,
            IConfiguration configuration,
            ILogger<PubSubSubscriberService> logger)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _projectId = configuration["GoogleMeet:ProjectId"] ?? throw new ArgumentNullException("GoogleMeet:ProjectId");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ListenForNotificationsAsync(string subscriptionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscriptionPath = SubscriptionName.FromProjectSubscription(_projectId, subscriptionName);

                // Create a SubscriberClient for simpler message pulling
                var subscriberClient = await SubscriberClient.CreateAsync(subscriptionPath);

                // Start listening for messages
                await subscriberClient.StartAsync(async (message, ct) =>
                {
                    await HandleMessageAsync(message, ct);
                    return SubscriberClient.Reply.Ack;
                });

                // Keep the subscriber running
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Notification listener was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error listening for notifications: {ex.Message}", ex);
            }
        }

        private async Task HandleMessageAsync(PubsubMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var messageText = message.Data.ToStringUtf8();
                var notification = JsonSerializer.Deserialize<NotificationResponseDto>(messageText);

                if (notification != null)
                {
                    // Send to connected user via SSE
                    await _connectionManager.SendNotificationAsync(notification.UserId, notification);
                    _logger.LogInformation($"Notification sent to user {notification.UserId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling PubSub message: {ex.Message}", ex);
            }
        }
    }
}