using SWD.BLL.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace SWD.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _sendGridApiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Read SendGrid API Key from Environment Variables
            _sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") 
                ?? throw new Exception("SENDGRID_API_KEY environment variable is required");
            
            _fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? "noreply@winmart-iot.com";
            _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? "Smart Weather Data Lab";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var client = new SendGridClient(_sendGridApiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            
            // Create message with HTML body
            var msg = MailHelper.CreateSingleEmail(
                from, 
                to, 
                subject, 
                plainTextContent: null,  // No plain text version
                htmlContent: body
            );

            // Send email via SendGrid API
            var response = await client.SendEmailAsync(msg);

            // Log if failed (but don't throw - fire and forget)
            if ((int)response.StatusCode >= 400)
            {
                Console.WriteLine($"SendGrid error: {response.StatusCode}");
            }
        }
    }
}