using SWD.BLL.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SWD.BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _fromEmail = "caohuutritl1234@gmail.com";
        private readonly string _appPassword = "thsdbqcdeauazife";  // Updated App Password

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_fromEmail, _appPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}