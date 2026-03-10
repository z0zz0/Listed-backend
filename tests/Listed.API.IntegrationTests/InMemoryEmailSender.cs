using Listed.Application.Contracts.Communication;

namespace Listed.API.IntegrationTests;

public sealed class InMemoryEmailSender : IEmailSender
{
    private readonly object _lock = new();
    private readonly List<EmailMessage> _messages = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_lock)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public EmailMessage? GetLatestMessage(string toAddress)
    {
        lock (_lock)
        {
            for (var index = _messages.Count - 1; index >= 0; index--)
            {
                var message = _messages[index];
                if (string.Equals(message.ToAddress, toAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return message;
                }
            }
        }

        return null;
    }

    public void Reset()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }
}
