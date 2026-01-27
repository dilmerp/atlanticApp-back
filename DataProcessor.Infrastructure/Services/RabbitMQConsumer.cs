using Common.Messages;
using DataProcessor.Application.Features.CargaMasiva.Commands;
using DataProcessor.Infrastructure.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; 
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System;
using System.Threading.Tasks;

namespace DataProcessor.Infrastructure.Services
{
    public class RabbitMQConsumer : IMessageConsumer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQConsumer> _logger; 

        private const string ExchangeName = "jobs.exchange";
        private const string QueueName = "FileProcessingQueue";

        
        public RabbitMQConsumer(
            IConnection connection,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMQConsumer> logger) 
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 

            _channel = _connection.CreateModel();
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                _channel.ExchangeDeclare(exchange: ExchangeName,
                                         type: ExchangeType.Direct,
                                         durable: true);

                _channel.QueueDeclare(queue: QueueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                // Asumo que la clave de ruteo vacía es la configuración deseada
                _channel.QueueBind(queue: QueueName,
                                         exchange: ExchangeName,
                                         routingKey: string.Empty);
            }
            catch (Exception ex)
            {
                // Ahora usamos el ILogger
                _logger.LogError(ex, "Error al configurar Exchange/Queue de RabbitMQ.");
                throw;
            }
        }

        public void StartConsuming()
        {
            if (_channel == null || !_connection.IsOpen)
            {
                _logger.LogError("ERROR: No se puede iniciar el consumo. La conexión o el canal no están abiertos.");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                bool processingSuccess = false;

                try
                {
                    _logger.LogInformation("[RabbitMQ Consumer] Mensaje JobCreatedEvent recibido: {Message}", message);

                    var jobEvent = JsonSerializer.Deserialize<JobCreatedEvent>(message,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var command = new ProcessFileCommand
                    {
                        CargaArchivoId = jobEvent.CargaArchivoId,
                        FileKey = jobEvent.FileKey
                    };

                    processingSuccess = await mediator.Send(command);

                    _logger.LogInformation("Procesamiento de Job {JobId} finalizado. Resultado: {Success}", jobEvent.CargaArchivoId, processingSuccess);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fatal al procesar el mensaje en el RabbitMQConsumer.");
                    processingSuccess = false;
                }

                if (processingSuccess)
                {
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                else
                {
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("[RabbitMQ] Consumidor iniciado en la cola: {QueueName}", QueueName);
        }

        public void StopConsuming()
        {
            _channel?.Close();
            _channel?.Dispose();
        }
    }
}