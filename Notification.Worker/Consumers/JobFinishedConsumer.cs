using Common.Messages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System; // Necesario para Exception

namespace Notification.Worker.Consumers
{
    public class JobFinishedConsumer
    {
        private readonly ILogger<JobFinishedConsumer> _logger;
        private readonly IModel _channel;

        private const string ExchangeName = "jobs.exchange";
        private const string QueueName = "job.finished.queue";
        private const string RoutingKey = "job.finished";

        
        public JobFinishedConsumer(
            ILogger<JobFinishedConsumer> logger,
            IConnection connection) 
        {
            _logger = logger;

            _channel = connection.CreateModel();

            // Configuración de Exchange/Queue/Binding
            try
            {
                _channel.ExchangeDeclare(
                    exchange: ExchangeName,
                    type: ExchangeType.Direct,
                    durable: true);

                _channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                _channel.QueueBind(
                    queue: QueueName,
                    exchange: ExchangeName,
                    routingKey: RoutingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar la topología de RabbitMQ para el consumidor de notificación.");
                throw;
            }

            _logger.LogInformation($"[RabbitMQ] Consumidor de notificación listo para escuchar en la cola: {QueueName}");
        }

        public void Start()
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    var evt = JsonSerializer.Deserialize<JobFinishedEvent>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    _logger.LogInformation(
                        " Notificación recibida | Usuario: {Email} | Carga: {CargaId} | Error: {ConErrores}",
                        evt!.UsuarioEmail,
                        evt.CargaArchivoId,
                        evt.ConErrores);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error procesando JobFinishedEvent. Se reencola.");

                    _channel.BasicNack(
                        ea.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false,
                consumer: consumer);
        }
    }
}