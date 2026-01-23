using FileIngestor.Application.Interfaces; // IMessagePublisher
using Common.Messages;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataProcessor.Worker.Workers.Services
{
    public class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        private const string ExchangeName = "jobs.exchange"; // Exchange para JobCreatedEvent (API -> DataProcessor)
        private const string RoutingKey = "FileProcessingQueue";

        public RabbitMqPublisher(IConfiguration configuration)
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration["rabbitmq:Host"] ?? "localhost",
                Port = int.Parse(configuration["rabbitmq:Port"] ?? "5672"),
                UserName = configuration["rabbitmq:UserName"] ?? "guest",
                Password = configuration["rabbitmq:Password"] ?? "guest"
            };

            int retryCount = 0;
            const int maxRetries = 5;

            while (retryCount < maxRetries)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    break;
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
                {
                    retryCount++;
                    Console.WriteLine($"Error de conexión con RabbitMQ (Intento {retryCount}/{maxRetries}): {ex.Message}. Reintentando en 3 segundos...");
                    Task.Delay(3000).Wait();
                }
            }

            if (_connection == null)
            {
                throw new InvalidOperationException("No se pudo conectar a RabbitMQ después de varios reintentos.");
            }

            _channel = _connection.CreateModel();

            // Declaración del Exchange principal de Jobs
            _channel.ExchangeDeclare(
              exchange: ExchangeName,
              type: ExchangeType.Direct,
              durable: true);

            // Declaración y binding de la cola
            _channel.QueueDeclare(
                queue: RoutingKey,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.QueueBind(
                queue: RoutingKey,
                exchange: ExchangeName,
                routingKey: RoutingKey);
        }

        // 1. Método Específico (Usado por la API FileIngestor)
        public Task PublishJobCreatedEventAsync(JobCreatedEvent jobEvent)
        {
            var message = JsonSerializer.Serialize(jobEvent);
            var body = Encoding.UTF8.GetBytes(message);
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(exchange: ExchangeName, routingKey: RoutingKey, basicProperties: properties, body: body);

            Console.WriteLine($"[RabbitMQ] Mensaje publicado en exchange '{ExchangeName}' con routingKey '{RoutingKey}'");

            return Task.CompletedTask;
        }

        // 2. Método Genérico (Usado por DataProcessor Worker para Notificaciones)
        public void Publish<T>(string exchangeName, string routingKey, T message) where T : class
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            Console.WriteLine($"[RabbitMQ] Mensaje publicado en exchange '{exchangeName}' con routingKey '{routingKey}'");
        }

        // --- Implementación de IDisposable ---
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _channel?.Close();
                    _connection?.Close();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
