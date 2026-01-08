using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Notification.Worker.Interfaces;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Notification.Worker.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendNotificationAsync(string? recipientEmail, int jobId, bool hadErrors)
        {
            if (string.IsNullOrEmpty(recipientEmail)) return;

            var message = new MimeMessage();
        
            string smtpHost = _configuration["SmtpSettings:Host"] ?? "smtp.mailtrap.io";
            int smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "2525");
            string smtpUser = _configuration["SmtpSettings:Username"] ?? "test_user";
            string smtpPass = _configuration["SmtpSettings:Password"] ?? "test_password";
            string senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "no-reply@atlantic.com";

            message.From.Add(new MailboxAddress("Casino Atlantic City", senderEmail));
            message.To.Add(new MailboxAddress("Usuario", recipientEmail));

            // Personalizar el asunto y cuerpo del correo
            string status = hadErrors ? "FINALIZADA CON ERRORES" : "FINALIZADA EXITOSAMENTE";
            string subject = $" Carga Masiva #{jobId} - {status}";
            string bodyText = $"Estimado usuario,\n\nLa carga masiva con ID: {jobId} ha terminado. El estado final es: {status}.";

            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = bodyText };

            try
            {
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    // Deshabilitar SSL para entornos de prueba como Mailtrap
                    await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR MAILKIT] No se pudo enviar el correo a {recipientEmail}: {ex.Message}");
            }
        }
    }
}