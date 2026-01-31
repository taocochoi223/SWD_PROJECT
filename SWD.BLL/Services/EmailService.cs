using SWD.BLL.Interfaces;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SWD.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;

            _apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                ?? throw new Exception("SENDGRID_API_KEY missing");

            _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM")
                ?? throw new Exception("EMAIL_FROM missing");

            _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                ?? "Smart Weather Data Lab";

            _logger.LogInformation($"EmailService initialized with SendGrid - From: {_fromEmail}, Name: {_fromName}, API Key: {(_apiKey.Length > 0 ? "***SET***" : "NOT SET")}");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            _logger.LogInformation($"Preparing to send email via SendGrid to {toEmail} with subject: {subject}");

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);

            _logger.LogInformation($"Sending email via SendGrid API to {toEmail}");
            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Email sent successfully via SendGrid to {toEmail}. Status: {response.StatusCode}");
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                _logger.LogError($"SendGrid API failed. Status: {response.StatusCode}, Error: {errorBody}");
                throw new Exception($"SendGrid failed with status {response.StatusCode}: {errorBody}");
            }
        }
    }
}
