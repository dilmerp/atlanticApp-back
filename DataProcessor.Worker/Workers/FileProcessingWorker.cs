using DataProcessor.Infrastructure.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Worker.Workers
{
    public class FileProcessingWorker : BackgroundService
    {
        private readonly IMessageConsumer _consumer;
        private readonly ILogger<FileProcessingWorker> _logger;

        public FileProcessingWorker(IMessageConsumer consumer, ILogger<FileProcessingWorker> logger)
        {
            _consumer = consumer;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FileProcessingWorker iniciado. Suscribiéndose a RabbitMQ...");
            _consumer.StartConsuming();
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FileProcessingWorker detenido.");
            _consumer.StopConsuming();
            return base.StopAsync(cancellationToken);
        }
    }
}
