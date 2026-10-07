using System.Threading.Channels;
using FairShare.Application.Abstractions;

namespace FairShare.Infrastructure.Email;

/// <summary>Ред порука у меморији; региструје се као Singleton.</summary>
public class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>();

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
