using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

using Listed.Application.Contracts.Communication;

namespace Listed.Infrastructure.Email;

public sealed class SmtpEmailSender(
    EmailOptions emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _emailOptions = emailOptions;
    private readonly ILogger<SmtpEmailSender> _logger = logger;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_emailOptions.FromAddress, _emailOptions.FromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsBodyHtml
        };

        mailMessage.To.Add(message.ToAddress);

        using var smtpClient = CreateSmtpClient();

        await smtpClient.SendMailAsync(mailMessage).WaitAsync(cancellationToken);

        _logger.LogInformation(
            "Email sent. FromAddress={FromAddress} ToAddress={ToAddress}, Subject={Subject}",
            mailMessage.From,
            message.ToAddress,
            message.Subject);
    }

    private SmtpClient CreateSmtpClient()
    {
        var smtpClient = new SmtpClient(_emailOptions.Host, _emailOptions.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _emailOptions.UseTls,
            UseDefaultCredentials = false
        };

        if (_emailOptions.ShouldUseAuthentication)
        {
            smtpClient.Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password);
        }

        return smtpClient;
    }
}
