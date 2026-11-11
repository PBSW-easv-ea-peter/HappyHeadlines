
namespace NewsletterService.Queues;

public interface IMessageHandler<in T> where T : class
{
    Task HandleAsync(T message, CancellationToken ct = default);
}
