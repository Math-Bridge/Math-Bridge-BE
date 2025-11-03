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

        public GooglePubSubNotificationProvider(IConfiguration configuration)
        {
            _publisherClient = PublisherServiceApiClient.Create();
            _projectId = configuration["GoogleMeet:ProjectId"] ?? throw new ArgumentNullException("GoogleMeet:ProjectId");
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
                        { "timestamp", DateTime.UtcNow.Ticks.ToString() }
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
                        { "timestamp", DateTime.UtcNow.Ticks.ToString() }
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
                var topicPath = TopicName.FromProjectTopic(_projectId, topicName);
                var subscriptionPath = SubscriptionName.FromProjectSubscription(_projectId, subscriptionName);

                var subscriberServiceApiClient = SubscriberServiceApiClient.Create();
                
                try
                {
                    await subscriberServiceApiClient.GetSubscriptionAsync(subscriptionPath);
                }
                catch (RpcException)
                {
                    var subscription = new Subscription
                    {
                        SubscriptionName = subscriptionPath,
                        TopicAsTopicName = topicPath
                    };
                    await subscriberServiceApiClient.CreateSubscriptionAsync(subscription);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to subscribe to PubSub topic {topicName}", ex);
            }
        }
    }
}