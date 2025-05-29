using System.Net;
using System.Net.Mail;
using Lombok.NET;
using Microsoft.Extensions.Options;
using Project.Server.Configs.Models;
using Project.Server.Services.Interfaces;

namespace SoluEmpleo.Server.Services.Core
{
    /// <summary>
    /// Defines the <see cref="SendEmail" />
    /// </summary>
    [AllArgsConstructor]
    public partial class SendEmail : ISendMail
    {
        /// <summary>
        /// Defines the _appSettings
        /// </summary>
        private readonly IOptions<AppSettings> _appSettings;

        /// <summary>
        /// Defines the _logger
        /// </summary>
        private readonly ILogger<SendEmail> _logger;

        /// <summary>
        /// The Send
        /// </summary>
        /// <param name="correo">The correo<see cref="string"/></param>
        /// <param name="asunto">The asunto<see cref="string"/></param>
        /// <param name="mensaje">The mensaje<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public bool Send(string correo, string asunto, string mensaje)
        {
            bool resultado;
            var appSettings = _appSettings.Value;
            try
            {
                MailMessage mail = new();
                mail.To.Add(correo);
                mail.From = new MailAddress(appSettings.Email);
                mail.Subject = asunto;
                mail.Body = mensaje;
                mail.IsBodyHtml = true;

                var smtp = new SmtpClient()
                {
                    Credentials = new NetworkCredential(appSettings.Email, appSettings.Password),
                    Host = appSettings.Host,
                    Port = appSettings.Port,
                    EnableSsl = true,
                };

                smtp.Send(mail);
                resultado = true;

            }
            catch (Exception ex)
            {
                resultado = false;

                _logger.LogError(ex, "Error al enviar el correo");
            }

            return resultado;
        }
    }
}
