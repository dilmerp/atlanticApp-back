using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Worker.Consumers;
using System.Threading;
using System.Threading.Tasks;

namespace Notification.Worker.Workers
{
    public class NotificationWorker : BackgroundService
    {
        private readonly ILogger<NotificationWorker> _logger;
        private readonly JobFinishedConsumer _consumer;

        public NotificationWorker(
            ILogger<NotificationWorker> logger,
            JobFinishedConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(" Notification Worker iniciado.");

            stoppingToken.Register(() =>
            {
                _logger.LogInformation(" Notification Worker detenido.");
            });

            _consumer.Start();

            return Task.CompletedTask;
        }
    }
}
