using Google.Cloud.PubSub.V1;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Services
{
    public class GooglePubSubNotificationProvider : IPubSubNotificationProvider
    {
        private readonly PublisherServiceApiClient _publisherClient;
        private readonly string _projectId;
        private readonly IConfiguration _configuration;

        public GooglePubSubNotificationProvider(IConfiguration configuration)
        {
            _configuration = configuration;
            _projectId = configuration["GoogleMeet:ProjectId"] ?? throw new ArgumentNullException("GoogleMeet:ProjectId");
            _publisherClient = CreatePublisherClientWithCredentials();
        }

        private PublisherServiceApiClient CreatePublisherClientWithCredentials()
        {
            try
            {
                var oauthJsonPath = _configuration["GoogleMeet:OAuthCredentialsPath"];
                
                if (string.IsNullOrEmpty(oauthJsonPath))
                {
                    Console.WriteLine("[WARNING] GoogleMeet:OAuthCredentialsPath not configured. Using default credentials.");
                    return PublisherServiceApiClient.Create();
                }

                if (!System.IO.Path.IsPathRooted(oauthJsonPath))
                {
                    oauthJsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), oauthJsonPath);
                }

                Console.WriteLine($"[DEBUG] Checking if PubSub OAuth credentials file exists: {oauthJsonPath}");
                if (!System.IO.File.Exists(oauthJsonPath))
                {
                    throw new System.IO.FileNotFoundException($"OAuth credentials file not found at: {oauthJsonPath}");
                }

                var googleCredential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(oauthJsonPath);
                Console.WriteLine($"[DEBUG] PubSub credentials loaded successfully from {oauthJsonPath}");
                
                return new PublisherServiceApiClientBuilder
                {
                    GoogleCredential = googleCredential
                }.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error loading PubSub credentials: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Error loading PubSub credentials: {ex.Message}", ex);
            }
        }

        private SubscriberServiceApiClient CreateSubscriberClientWithCredentials()
        {
            try
            {
                var oauthJsonPath = _configuration["GoogleMeet:OAuthCredentialsPath"];
                
                if (string.IsNullOrEmpty(oauthJsonPath))
                {
                    Console.WriteLine("[WARNING] GoogleMeet:OAuthCredentialsPath not configured. Using default credentials.");
                    return SubscriberServiceApiClient.Create();
                }

                if (!System.IO.Path.IsPathRooted(oauthJsonPath))
                {
                    oauthJsonPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), oauthJsonPath);
                }

                Console.WriteLine($"[DEBUG] Checking if Subscriber OAuth credentials file exists: {oauthJsonPath}");
                if (!System.IO.File.Exists(oauthJsonPath))
                {
                    throw new System.IO.FileNotFoundException($"OAuth credentials file not found at: {oauthJsonPath}");
                }

                var googleCredential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(oauthJsonPath);
                Console.WriteLine($"[DEBUG] Subscriber credentials loaded successfully from {oauthJsonPath}");
                
                return new SubscriberServiceApiClientBuilder
                {
                    GoogleCredential = googleCredential
                }.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error loading Subscriber credentials: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Error loading Subscriber credentials: {ex.Message}", ex);
            }
        }

        public async Task PublishNotificationAsync(NotificationResponseDto notification, string topicName)
        {
            try
            {
                var topic = new TopicName(_projectId, topicName);
                var messageJson = JsonSerializer.Serialize(notification);
                var pubsubMessage = new PubsubMessage
                {
                    Data = Google.Protobuf.ByteString.CopyFromUtf8(messageJson),
                    Attributes =
                    {
                        { "userId", notification.UserId.ToString() },
                        { "notificationType", notification.NotificationType },
                        { "timestamp", DateTime.UtcNow.ToLocalTime().Ticks.ToString() }
                    }
                };

                await _publisherClient.PublishAsync(topic, new[] { pubsubMessage });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to publish notification to PubSub topic {topicName}", ex);
            }
        }

        public async Task PublishBatchNotificationsAsync(List<NotificationResponseDto> notifications, string topicName)
        {
            try
            {
                var topic = new TopicName(_projectId, topicName);
                var messages = notifications.Select(n => new PubsubMessage
                {
                    Data = Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(n)),
                    Attributes =
                    {
                        { "userId", n.UserId.ToString() },
                        { "notificationType", n.NotificationType },
                        { "timestamp", DateTime.UtcNow.ToLocalTime().Ticks.ToString() }
                    }
                }).ToList();

                await _publisherClient.PublishAsync(topic, messages);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to publish batch notifications to PubSub topic {topicName}", ex);
            }
        }

        public async Task<bool> TopicExistsAsync(string topicName)
        {
            try
            {
                var topic = new TopicName(_projectId, topicName);
                await _publisherClient.GetTopicAsync(topic);
                return true;
            }
            catch (RpcException)
            {
                return false;
            }
        }

        public async Task SubscribeAsync(string topicName, string subscriptionName)
        {
            try
            {
                Console.WriteLine($"[DEBUG] SubscribeAsync: Creating subscription '{subscriptionName}' for topic '{topicName}'");
                
                var topicPath = TopicName.FromProjectTopic(_projectId, topicName);
                var subscriptionPath = SubscriptionName.FromProjectSubscription(_projectId, subscriptionName);

                Console.WriteLine($"[DEBUG] SubscribeAsync: Topic path: {topicPath}, Subscription path: {subscriptionPath}");
                var subscriberServiceApiClient = CreateSubscriberClientWithCredentials();
                
                try
                {
                    Console.WriteLine($"[DEBUG] SubscribeAsync: Attempting to get existing subscription");
                    await subscriberServiceApiClient.GetSubscriptionAsync(subscriptionPath);
                    Console.WriteLine($"[DEBUG] SubscribeAsync: Subscription already exists");
                }
                catch (RpcException rpcEx) when (rpcEx.StatusCode == StatusCode.NotFound)
                {
                    Console.WriteLine($"[DEBUG] SubscribeAsync: Subscription not found, creating new one");
                    var subscription = new Subscription
                    {
                        SubscriptionName = subscriptionPath,
                        TopicAsTopicName = topicPath
                    };
                    var createdSub = await subscriberServiceApiClient.CreateSubscriptionAsync(subscription);
                    Console.WriteLine($"[DEBUG] SubscribeAsync: Subscription created successfully: {createdSub.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SubscribeAsync failed: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to subscribe to PubSub topic {topicName}: {ex.Message}", ex);
            }
        }
        }
    }