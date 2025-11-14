using MathBridgeSystem.Application.DTOs.Notification;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Services
{
    public class NotificationConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, StreamWriter> _userConnections;
        private readonly ConcurrentDictionary<Guid, List<StreamWriter>> _notificationQueues;

        public NotificationConnectionManager()
        {
            _userConnections = new ConcurrentDictionary<Guid, StreamWriter>();
            _notificationQueues = new ConcurrentDictionary<Guid, List<StreamWriter>>();
        }

        public void RegisterConnection(Guid userId, StreamWriter writer)
        {
            _userConnections.AddOrUpdate(userId, writer, (key, oldValue) => writer);
        }

        public async Task UnregisterConnectionAsync(Guid userId)
        {
            _userConnections.TryRemove(userId, out var writer);
            if (writer != null)
            {
                await writer.FlushAsync();
                await writer.DisposeAsync();
            }
        }

        public async Task SendNotificationAsync(Guid userId, NotificationResponseDto notification)
        {
            if (_userConnections.TryGetValue(userId, out var writer))
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(notification);
                    var sseMessage = $"data: {json}\n\n";
                    await writer.WriteAsync(sseMessage);
                    await writer.FlushAsync();
                }
                catch (Exception)
                {
                    await UnregisterConnectionAsync(userId);
                }
            }
        }

        public async Task BroadcastNotificationAsync(NotificationResponseDto notification, IEnumerable<Guid> userIds)
        {
            var tasks = userIds.Select(userId => SendNotificationAsync(userId, notification));
            await Task.WhenAll(tasks);
        }

        public int GetActiveConnectionCount()
        {
            return _userConnections.Count;
        }

        public int GetActiveConnectionCountForUser(Guid userId)
        {
            return _userConnections.ContainsKey(userId) ? 1 : 0;
        }

        public IEnumerable<Guid> GetAllConnectedUsers()
        {
            return _userConnections.Keys;
        }
    }
}