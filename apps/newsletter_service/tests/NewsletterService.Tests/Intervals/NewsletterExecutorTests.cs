using Microsoft.Extensions.Logging;
using Moq;
using NewsletterService.NewsletterDistributors.EmailService;
using NewsletterService.NewsletterManager;
using NewsletterService.Services.ArticleService;
using NewsletterService.Services.SubscriberService;
using NewsletterService.Shared;
using NewsletterService.Shared.Options;
using Microsoft.Extensions.Time.Testing;

namespace NewsletterService.Tests.Intervals;

public class NewsletterExecutorTests
{
    private readonly Mock<ILogger<NewsletterExecutor>> _loggerMock = new();
    private readonly Mock<ISubscriberService> _subscriberServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IEmailFactory> _emailFactoryMock = new();
    private readonly Mock<IArticleService> _articleServiceMock = new();
    private readonly FakeTimeProvider _fakeTimeProvider;

    public NewsletterExecutorTests()
    {
        // Set up a fake time provider starting at a known date/time
        // Using November 6, 2026 at 07:00:00 AM
        _fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 11, 6, 7, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ShouldExecuteNow_DailyInterval_At7AM_ReturnsFalse()
    {
        // Arrange
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Daily,
            HourOfDay = 8  // Configured to run at 8 AM
        };

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act - Time is currently 7:00 AM
        bool shouldExecute = executor.ShouldExecuteNow();

        // Assert
        Assert.False(shouldExecute);
    }

    [Fact]
    public void ShouldExecuteNow_DailyInterval_At8AM_ReturnsTrue()
    {
        // Arrange
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Daily,
            HourOfDay = 8  // Configured to run at 8 AM
        };

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act - Advance time to 8:00 AM
        _fakeTimeProvider.Advance(TimeSpan.FromHours(1));
        bool shouldExecute = executor.ShouldExecuteNow();

        // Assert
        Assert.True(shouldExecute);
    }

    [Fact]
    public void ShouldExecuteNow_DailyInterval_At8AMWithMinutes_ReturnsFalse()
    {
        // Arrange
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Daily,
            HourOfDay = 8  // Configured to run at 8 AM
        };

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act - Advance time to 8:30 AM (30 minutes past the hour)
        _fakeTimeProvider.Advance(TimeSpan.FromHours(1.5));
        bool shouldExecute = executor.ShouldExecuteNow();

        // Assert - Should not execute because minute is not 0
        Assert.False(shouldExecute);
    }

    [Fact]
    public void ShouldExecuteNow_WeeklyInterval_WrongDay_ReturnsFalse()
    {
        // Arrange - November 6, 2026 is a Friday
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Weekly,
            DayOfWeek = DayOfWeek.Thursday,  // Configured to run on Friday
            HourOfDay = 7
        };

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act - Time is currently Thursday 7:00 AM
        bool shouldExecute = executor.ShouldExecuteNow();

        // Assert - Should not execute because it's not Friday
        Assert.False(shouldExecute);
    }

    [Fact]
    public void ShouldExecuteNow_WeeklyInterval_CorrectDayAndHour_ReturnsTrue()
    {
        // Arrange - November 6, 2026 is a Friday
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Weekly,
            DayOfWeek = DayOfWeek.Friday,  // Configured to run on Thursday
            HourOfDay = 8
        };

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act - Advance time to Thursday 8:00 AM
        _fakeTimeProvider.Advance(TimeSpan.FromHours(1));
        bool shouldExecute = executor.ShouldExecuteNow();

        // Assert - Should execute because it's Thursday at 8 AM
        Assert.True(shouldExecute);
    }

    [Fact]
    public void ShouldExecuteNow_WeeklyInterval_NoDayOfWeek_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Weekly,
            DayOfWeek = null,
            HourOfDay = 8
        };

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => executor.ShouldExecuteNow());

        Assert.Equal("DayOfWeek", exception.ParamName);
    }
}
