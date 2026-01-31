using System.Net;
using System.Net.Mail;
using SWD.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace SWD.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _email;
        private readonly string _password;
        private readonly string _fromName;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;

            _email = Environment.GetEnvironmentVariable("EMAIL_FROM")
                ?? throw new Exception("EMAIL_FROM missing");

            _password = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD")
                ?? throw new Exception("EMAIL_APP_PASSWORD missing");

            _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                ?? "Smart Weather Data Lab";

            _logger.LogInformation($"EmailService initialized - From: {_email}, Name: {_fromName}, Password: {(_password.Length > 0 ? "***SET***" : "NOT SET")}");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation($"Preparing to send email to {toEmail} with subject: {subject}");

            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_email, _password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_email, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            _logger.LogInformation($"Attempting SMTP connection to smtp.gmail.com:587 for {toEmail}");
            await smtpClient.SendMailAsync(mail);
            _logger.LogInformation($"Email sent successfully via SMTP to {toEmail}");
        }
    }
}
