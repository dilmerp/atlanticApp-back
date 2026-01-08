using System.Threading.Tasks;

namespace Notification.Worker.Interfaces
{
    public interface IEmailService
    {
        Task SendNotificationAsync(string? recipientEmail, int jobId, bool hadErrors);
    }
}