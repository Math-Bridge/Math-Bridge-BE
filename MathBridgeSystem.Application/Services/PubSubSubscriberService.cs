using Google.Cloud.PubSub.V1;
using Google.Apis.Auth.OAuth2;
using MathBridgeSystem.Application.DTOs.Notification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Services
{
    public class PubSubSubscriberService
    {
        private readonly NotificationConnectionManager _connectionManager;
        private readonly string _projectId;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PubSubSubscriberService> _logger;

        public PubSubSubscriberService(
            NotificationConnectionManager connectionManager,
            IConfiguration configuration,
            ILogger<PubSubSubscriberService> logger)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _projectId = configuration["GoogleMeet:ProjectId"] ?? throw new ArgumentNullException("GoogleMeet:ProjectId");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ListenForNotificationsAsync(string subscriptionName, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscriptionPath = SubscriptionName.FromProjectSubscription(_projectId, subscriptionName);

                // Setup OAuth credentials from JSON
                var oauthJsonPath = _configuration["GoogleMeet:OAuthCredentialsPath"];
                if (string.IsNullOrEmpty(oauthJsonPath))
                {
                    throw new InvalidOperationException("GoogleMeet:OAuthCredentialsPath configuration is missing");
                }

                // Resolve relative path if needed
                if (!Path.IsPathRooted(oauthJsonPath))
                {
                    oauthJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, oauthJsonPath);
                }

                if (!File.Exists(oauthJsonPath))
                {
                    throw new FileNotFoundException($"OAuth credentials file not found at: {oauthJsonPath}");
                }

                _logger.LogInformation($"Loading Google credentials from: {oauthJsonPath}");

                // Load credentials from OAuth JSON file
                string jsonCredentials;
                using (var stream = File.OpenRead(oauthJsonPath))
                using (var reader = new StreamReader(stream))
                {
                    jsonCredentials = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrEmpty(jsonCredentials))
                {
                    throw new InvalidOperationException("OAuth credentials file is empty");
                }

                _logger.LogInformation("Successfully loaded Google credentials from OAuth JSON");

                // Create SubscriberClient using builder with JSON credentials
                var subscriberClient = new SubscriberClientBuilder
                {
                    SubscriptionName = subscriptionPath,
                    JsonCredentials = jsonCredentials
                }.Build();

                _logger.LogInformation($"Connected to Pub/Sub subscription: {subscriptionName}");

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
                throw;
            }
        }

        private async Task HandleMessageAsync(PubsubMessage message, CancellationToken cancellationToken)
        {
            try
            {
                var messageText = message.Data.ToStringUtf8();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var notification = JsonSerializer.Deserialize<NotificationResponseDto>(messageText, options);

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
