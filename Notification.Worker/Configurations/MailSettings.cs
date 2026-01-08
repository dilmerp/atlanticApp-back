
namespace Notification.Worker.Configurations
{
    public class MailSettings
    {
        
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "AtlanticApp System";
    }
}