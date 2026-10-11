using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Project.Application.Abstractions;

namespace Project.Infrastructure.Services;

/// <summary><c>Email</c> section. When <see cref="Host"/> is empty, emails are written to the log.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    [EmailAddress]
    public string From { get; set; } = "no-reply@example.com";
}

/// <summary>SMTP sender (works with Mailpit in development and any SMTP relay in production).</summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrEmpty(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        using var message = new MailMessage(settings.From, to, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, ct);
    }
}
