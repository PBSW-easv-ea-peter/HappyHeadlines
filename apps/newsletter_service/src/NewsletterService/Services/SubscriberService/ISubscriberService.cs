namespace NewsletterService.Services.SubscriberService;

public interface ISubscriberService
{
    public Task<Subscriber[]> GetSubscriptionsAsync(int interval);
}

public class InMemorySubscriberService : ISubscriberService
{
    public Task<Subscriber[]> GetSubscriptionsAsync(int interval)
    {
        return interval switch
        {
            0 => Task.FromResult(FakeSubScribers.Data[..2]),
            1 => Task.FromResult(FakeSubScribers.Data[..1]),
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
}