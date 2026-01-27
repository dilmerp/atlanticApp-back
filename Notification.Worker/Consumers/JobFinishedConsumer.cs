using Common.Domain.Entities;
using Common.Domain.Interfaces;
using Common.Domain.Enums;
using Common.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Worker.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; 

namespace Notification.Worker.Consumers
{
    public class JobFinishedConsumer
    {
        private readonly ILogger<JobFinishedConsumer> _logger;
        private readonly IModel _channel;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory; 

        // Correcciones de Ruteo
        private const string ExchangeName = "notifications.exchange";
        private const string QueueName = "notification.finished.queue";
        private const string RoutingKey = "notificaciones";

        public JobFinishedConsumer(
            ILogger<JobFinishedConsumer> logger,
            IConnection connection,
            IEmailService emailService,
            IServiceScopeFactory scopeFactory) 
        {
            _logger = logger;
            _emailService = emailService;
            _scopeFactory = scopeFactory; 
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

            consumer.Received += async (model, ea) =>
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

                    if (evt == null) throw new InvalidOperationException("Evento deserializado es nulo.");

                    _logger.LogInformation(
                        "Notificación recibida | Usuario: {Email} | Carga: {CargaId} | Error: {ConErrores}",
                        evt.UsuarioEmail,
                        evt.CargaArchivoId,
                        evt.ConErrores);

                    //  INICIO DEL SCOPE: Todo lo que use servicios Scoped (como DbContext) debe ir dentro.
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        // Obtener el DbContext para este mensaje específico
                        var scopedContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                        // 1. Enviar Correo (IEmailService)
                        await _emailService.SendNotificationAsync(
                            evt.UsuarioEmail,
                            evt.CargaArchivoId,
                            evt.ConErrores);

                        _logger.LogInformation("Correo de notificación enviado para Carga ID: {CargaId}", evt.CargaArchivoId);

                        // 2. Actualizar estado en la base de datos (Usando scopedContext)
                        var carga = await scopedContext.CargaArchivos 
                            .FirstOrDefaultAsync(c => c.Id == evt.CargaArchivoId);

                        if (carga != null)
                        {
                            // LOG DE DEBUGGING CRÍTICO
                            _logger.LogInformation("DEBUG: Estado actual de Carga {CargaId} antes de actualizar: {Estado}", evt.CargaArchivoId, carga.Estado);

                            if (carga.Estado == EstadoCarga.Finalizado)
                            {
                                carga.Estado = EstadoCarga.Notificado;
                                int rowsAffected = await scopedContext.SaveChangesAsync(); 

                                if (rowsAffected > 0)
                                {
                                    _logger.LogInformation("Estado de Carga ID {CargaId} actualizado a NOTIFICADO. Filas afectadas: {Rows}", evt.CargaArchivoId, rowsAffected);
                                }
                                else
                                {
                                    _logger.LogWarning("ERROR: SaveChangesAsync no reportó filas afectadas. Puede ser un error de conexión a DB. Carga ID: {CargaId}", evt.CargaArchivoId);
                                }
                            }
                            else if (carga.Estado == EstadoCarga.Error)
                            {
                                _logger.LogWarning("La carga {CargaId} está en estado ERROR. Se mantiene el estado.", evt.CargaArchivoId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("CargaArchivo {CargaId} no encontrado.", evt.CargaArchivoId);
                        }
                    } // El scope se libera aquí.

                    // Acknowledge (ACK) solo después de completar toda la lógica de negocio
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error procesando JobFinishedEvent. Se reencola.");

                    // BasicNack (requeue: true) para reintentar
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