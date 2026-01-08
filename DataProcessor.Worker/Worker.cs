using DataProcessor.Infrastructure.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataProcessor.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IMessageConsumer _messageConsumer; 

    // Constructor actualizado
    public Worker(ILogger<Worker> logger, IMessageConsumer messageConsumer)
    {
        _logger = logger;
        _messageConsumer = messageConsumer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RabbitMQ Consumer Service...");

        // Método que inicia la escucha de la cola.
        _messageConsumer.StartConsuming();

        _logger.LogInformation("RabbitMQ Consumer started and is now listening.");

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping RabbitMQ Consumer Service...");
        _messageConsumer.StopConsuming();
        await base.StopAsync(stoppingToken);
    }
}