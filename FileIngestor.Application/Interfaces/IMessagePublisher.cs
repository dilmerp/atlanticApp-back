using Common.Messages;
using System.Threading.Tasks;

namespace FileIngestor.Application.Interfaces
{
    public interface IMessagePublisher
    {
        /// <summary>
        /// Publica un evento de creación de trabajo a la cola de mensajes (RabbitMQ).
        /// </summary>
        Task PublishJobCreatedEventAsync(JobCreatedEvent jobEvent);

        /// </summary>
        void Publish<T>(string exchangeName, string routingKey, T message) where T : class;
    }
}