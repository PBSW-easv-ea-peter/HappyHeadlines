using Microsoft.Extensions.Options;
using NewsletterService.Shared.Options;

namespace NewsletterService.Intervals;

public class DailyNewsletterService : INewsletterService
{
    private readonly IntervalOptions _options;
    private readonly ILogger<DailyNewsletterService> _logger;
    private readonly TimeProvider _timeProvider;

    public DailyNewsletterService(
        IOptions<IntervalOptions> options,
        ILogger<DailyNewsletterService> logger,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }
    
    public string Interval => _options.IntervalName;
    
    public Task Delay(CancellationToken stoppingToken)
    {
        DateTime now = _timeProvider.GetLocalNow().DateTime;

        // Calculate the next execution time
        DateTime nextRun = now.Date.AddHours(_options.HourOfDay);

        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);

        TimeSpan delay = nextRun - now;

        _logger.LogInformation(
            "Next execution scheduled for {NextRun}",
            nextRun);

        return Task.Delay(delay, _timeProvider, stoppingToken);
    }
    
    public async Task ExecuteTask(CancellationToken cancellationToken)
    {
        DateTime now = _timeProvider.GetLocalNow().DateTime;
        
        _logger.LogInformation(
            "Executing daily task at {Time}",
            now);

        // Your logic here

        await Task.CompletedTask;
    }
}
