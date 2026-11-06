namespace NewsletterService.Intervals;

public interface INewsletterService
{
    public string Interval { get; }
    public Task Delay(CancellationToken stoppingToken);
    public Task ExecuteTask(CancellationToken cancellationToken);
}