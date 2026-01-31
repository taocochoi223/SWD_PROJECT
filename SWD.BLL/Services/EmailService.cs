using System.Net;
using System.Net.Mail;
using SWD.BLL.Interfaces;

namespace SWD.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _email;
        private readonly string _password;
        private readonly string _fromName;

        public EmailService()
        {
            _email = Environment.GetEnvironmentVariable("EMAIL_FROM")
                ?? throw new Exception("EMAIL_FROM missing");

            _password = Environment.GetEnvironmentVariable("EMAIL_APP_PASSWORD")
                ?? throw new Exception("EMAIL_APP_PASSWORD missing");

            _fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME")
                ?? "Smart Weather Data Lab";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
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

            await smtpClient.SendMailAsync(mail);
        }
    }
}
