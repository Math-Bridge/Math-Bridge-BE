using MathBridgeSystem.Application.DTOs.Notification;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces;

public interface IPubSubNotificationProvider
{
    Task PublishNotificationAsync(NotificationResponseDto notification, string topicName);
    Task PublishBatchNotificationsAsync(List<NotificationResponseDto> notifications, string topicName);
    Task<bool> TopicExistsAsync(string topicName);
    Task SubscribeAsync(string topicName, string subscriptionName);
}