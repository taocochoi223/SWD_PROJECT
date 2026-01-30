using SWD.BLL.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SWD.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly bool _enableSsl;
        private readonly string _emailFrom;

        public EmailService()
        {
            // Read all config from Environment Variables with fallbacks
            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out int port) ? port : 587;
            _smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? "caohuutritl1234@gmail.com";
            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "thsd bqcd eaua zife";
            _enableSsl = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL") != "false"; // Default true
            _emailFrom = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "caohuutritl1234@gmail.com";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(_smtpHost)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailFrom),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}