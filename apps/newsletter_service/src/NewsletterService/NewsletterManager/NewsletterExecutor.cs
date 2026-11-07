using NewsletterService.NewsletterDistributors.EmailService;
using NewsletterService.Services.ArticleService;
using NewsletterService.Services.SubscriberService;
using NewsletterService.Shared;
using NewsletterService.Shared.Options;

namespace NewsletterService.NewsletterManager;

/// <summary>
/// Executes newsletter tasks for a specific interval configuration
/// </summary>
public class NewsletterExecutor
{
    private readonly IntervalOptions _options;
    private readonly ILogger<NewsletterExecutor> _logger;
    private readonly TimeProvider _timeProvider;
    
    private readonly ISubscriberService _subscriberService;
    private readonly IEmailService _emailService;
    private readonly IEmailFactory _emailFactory;
    
    private readonly IArticleService _articleService;

    public NewsletterExecutor(
        IntervalOptions options,
        ILogger<NewsletterExecutor> logger,
        TimeProvider timeProvider,
        ISubscriberService subscriberService,
        IEmailService emailService,
        IEmailFactory emailFactory,
        IArticleService articleService)
    {
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
        
        _subscriberService = subscriberService;
        _emailService = emailService;
        _emailFactory = emailFactory;
        
        _articleService = articleService;
    }

    /// <summary>
    /// Checks if this interval should be executed at the current time
    /// </summary>
    public bool ShouldExecuteNow()
    {
        DateTime now = _timeProvider.GetLocalNow().DateTime;
        
        if (_options.Interval == NewsletterIntervals.Daily)
        {
            return now.Hour == _options.HourOfDay && now.Minute == 0;
        }
        else if (_options.Interval == NewsletterIntervals.Weekly)
        {
            if (!_options.DayOfWeek.HasValue)
                throw new ArgumentNullException(nameof(_options.DayOfWeek));
            
            return now.DayOfWeek == _options.DayOfWeek.Value && 
                   now.Hour == _options.HourOfDay && 
                   now.Minute == 0;
        }
        
        return false;
    }

    /// <summary>
    /// Executes the newsletter task for this interval
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTime now = _timeProvider.GetLocalNow().DateTime;
        
        _logger.LogInformation(
            "Executing {Interval} newsletter task at {Time}",
            _options.Interval,
            now);

        // Get subscriptions
        Subscriber[] subscriptions = await _subscriberService.GetSubscriptionsAsync((int)_options.Interval);

        // Get Articles since last newsletter
        int offsetDays = (_options.Interval) switch
        {
            NewsletterIntervals.Daily => 1,
            NewsletterIntervals.Weekly => 7,
            _ => throw new InvalidOperationException($"Unknown interval: {_options.Interval}")
        };
        DateTimeOffset lastNewsletter = now - new TimeSpan(offsetDays);
        
        IReadOnlyCollection<Article> articles = await _articleService.GetArticlesSinceAsync(lastNewsletter);
        
        IEnumerable<Email> emails = [.. subscriptions.Select(sub => _emailFactory.CreateDailyNewsletterAsync(sub, articles).Result)];

        _emailService.SendEmails(emails);
        
        _logger.LogInformation(
            "Completed {Interval} newsletter task at {Time}",
            _options.Interval,
            _timeProvider.GetLocalNow());
    }
}
