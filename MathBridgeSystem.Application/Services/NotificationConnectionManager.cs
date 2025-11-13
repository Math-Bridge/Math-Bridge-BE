using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs.Notification;

namespace MathBridgeSystem.Infrastructure.Services
{
    public class NotificationConnectionManager
    {
        private readonly ConcurrentDictionary<Guid, StreamWriter> _userConnections;

        public NotificationConnectionManager()
        {
            _userConnections = new ConcurrentDictionary<Guid, StreamWriter>();
        }

        public void RegisterConnection(Guid userId, StreamWriter writer)
        {
            // Add or replace the writer for the user
            _userConnections.AddOrUpdate(userId, writer, (key, oldValue) => writer);
        }

        public async Task UnregisterConnectionAsync(Guid userId)
        {
            // Remove and dispose the writer if present
            _userConnections.TryRemove(userId, out var writer);
            if (writer == null)
                return;

            try
            {
                await writer.FlushAsync();
            }
            catch
            {
                // ignore
            }

            try
            {
                await writer.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }

        public async Task SendNotificationAsync(Guid userId, NotificationResponseDto notification)
        {
            if (!_userConnections.TryGetValue(userId, out var writer))
                return;

            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(notification);
                var sseMessage = $"data: {json}\n\n";

                await writer.WriteAsync(sseMessage);
                await writer.FlushAsync();
            }
            catch (ObjectDisposedException)
            {
                await UnregisterConnectionAsync(userId);
            }
            catch (IOException)
            {
                await UnregisterConnectionAsync(userId);
            }
            catch (Exception)
            {
                await UnregisterConnectionAsync(userId);
            }
        }

        public async Task BroadcastNotificationAsync(NotificationResponseDto notification, IEnumerable<Guid> userIds)
        {
            if (userIds == null)
                return;

            var tasks = userIds.Select(u => SendNotificationAsync(u, notification));
            await Task.WhenAll(tasks);
        }

        public int GetActiveConnectionCount() => _userConnections.Count;

        public int GetActiveConnectionCountForUser(Guid userId) => _userConnections.ContainsKey(userId) ? 1 : 0;

        public IEnumerable<Guid> GetAllConnectedUsers() => _userConnections.Keys.ToList();
    }
}
