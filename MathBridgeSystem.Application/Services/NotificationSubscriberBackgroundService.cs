using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using MathBridgeSystem.Infrastructure.Services;

namespace MathBridgeSystem.Application.Services
{
    public class NotificationSubscriberBackgroundService : BackgroundService
    {
        private readonly PubSubSubscriberService _pubSubSubscriberService;
        private readonly ILogger<NotificationSubscriberBackgroundService> _logger;
        private const string SubscriptionName = "session";

        public NotificationSubscriberBackgroundService(
            PubSubSubscriberService pubSubSubscriberService,
            ILogger<NotificationSubscriberBackgroundService> logger)
        {
            _pubSubSubscriberService = pubSubSubscriberService ?? throw new ArgumentNullException(nameof(pubSubSubscriberService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationSubscriberBackgroundService starting.");

            try
            {
                await _pubSubSubscriberService.ListenForNotificationsAsync(SubscriptionName, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("NotificationSubscriberBackgroundService was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in NotificationSubscriberBackgroundService: {ex.Message}", ex);
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NotificationSubscriberBackgroundService stopping.");
            await base.StopAsync(cancellationToken);
        }
    }
}