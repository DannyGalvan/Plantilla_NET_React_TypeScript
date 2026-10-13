using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;
using Project.Application.Configuration;

namespace Project.Infrastructure.Services;

/// <summary><c>Email</c> section, editable at runtime. When <see cref="Host"/> is empty, emails are written to the log.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [ConfigurationHelp("SMTP server. Empty: emails are written to the log (development).")]
    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    [Secret]
    public string? Password { get; set; }

    [EmailAddress]
    public string From { get; set; } = "no-reply@example.com";
}

/// <summary>
/// Sends through SMTP when <c>Email:Host</c> is configured and to the log otherwise. Reads the settings on every message,
/// so SMTP configured in the configuration page works without a restart (Mailpit in development, any relay in production).
/// </summary>
public sealed class SmtpEmailSender(IOptionsMonitor<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        var settings = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            logger.LogInformation("Email to {To} (no SMTP configured): {Subject}\n{Body}", to, subject, htmlBody);
            return;
        }

        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrEmpty(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        using var message = new MailMessage(settings.From, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, ct);
    }
}
