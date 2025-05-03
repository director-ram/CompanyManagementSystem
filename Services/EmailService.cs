using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace CompanyManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var server = smtpSettings["Server"] ?? throw new InvalidOperationException("SMTP Server not configured");
                var port = smtpSettings["Port"] ?? throw new InvalidOperationException("SMTP Port not configured");
                var username = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username not configured");
                var password = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
                var fromEmail = smtpSettings["FromEmail"] ?? throw new InvalidOperationException("From Email not configured");
                var fromName = smtpSettings["FromName"] ?? "Purchase Order System";

                using var client = new SmtpClient()
                {
                    Host = server,
                    Port = int.Parse(port),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };

                using var message = new MailMessage()
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email: {ex.Message}");
                throw;
            }
        }
    }
}