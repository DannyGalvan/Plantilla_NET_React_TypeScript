using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Project.Server.Configs.Models;
using Project.Server.Services.Interfaces;

namespace Project.Server.Services.Core
{
    /// <summary>
    /// SMTP-backed email sender.
    /// </summary>
    public class SendEmail : ISendMail
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<SendEmail> _logger;

        public SendEmail(IOptions<AppSettings> appSettings, ILogger<SendEmail> logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            try
            {
                var settings = _appSettings.Value;
                using var mail = new MailMessage();
                mail.To.Add(to);
                mail.From = new MailAddress(settings.Email);
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true;

                using var smtp = new SmtpClient
                {
                    Credentials = new NetworkCredential(settings.Email, settings.Password),
                    Host = settings.Host,
                    Port = settings.Port,
                    EnableSsl = true,
                };

                await smtp.SendMailAsync(mail, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendAsync failed for {To}", to);
                return false;
            }
        }
    }
}