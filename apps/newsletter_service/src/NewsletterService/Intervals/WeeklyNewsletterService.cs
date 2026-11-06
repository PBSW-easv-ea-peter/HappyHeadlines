using Microsoft.Extensions.Options;
using NewsletterService.Shared.Options;

namespace NewsletterService.Intervals;

public class WeeklyNewsletterService : INewsletterService
{
    private readonly IntervalOptions _options;
    private readonly ILogger<WeeklyNewsletterService> _logger;
    private readonly TimeProvider _timeProvider;

    public WeeklyNewsletterService(
        IOptions<IntervalOptions> options,
        ILogger<WeeklyNewsletterService> logger,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Interval => _options.IntervalName;

    public Task Delay(CancellationToken stoppingToken)
    {
        if (!_options.DayOfWeek.HasValue)
            throw new InvalidOperationException("WeeklyNewsletter options are not configured");
        
        DateTime now = _timeProvider.GetLocalNow().DateTime;

        // Calculate the next execution time
        DateTime nextRun = now.Date
            .AddDays(((_options.DayOfWeek.Value - now.DayOfWeek + 7) % 7))
            .AddHours(_options.HourOfDay);

        if (now >= nextRun)
            nextRun = nextRun.AddDays(7);

        TimeSpan delay = nextRun - now;

        _logger.LogInformation(
            "Next execution scheduled for {NextRun}",
            nextRun);

        return Task.Delay(delay, _timeProvider, stoppingToken);
    }

    public async Task ExecuteTask(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing weekly task at {Time}",
            _timeProvider.GetLocalNow());

        // Your logic here

        await Task.CompletedTask;
    }
}
