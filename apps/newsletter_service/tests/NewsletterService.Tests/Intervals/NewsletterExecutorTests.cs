using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly Mock<IOptionsMonitor<FeatureTogglesOptions>> _togglesMock = new();
    private readonly FakeTimeProvider _fakeTimeProvider;

    public NewsletterExecutorTests()
    {
        // Set up a fake time provider starting at a known date/time
        // Using November 6, 2026 at 07:00:00 AM
        _fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 11, 6, 7, 0, 0, TimeSpan.Zero));

        // Set up default feature toggles
        var featureToggles = new FeatureTogglesOptions
        {
            Toggles = new List<FeatureToggle>
            {
                new FeatureToggle { Name = "UseSubscriberService", State = true }
            }
        };
        _togglesMock.Setup(x => x.CurrentValue).Returns(featureToggles);
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
            _togglesMock.Object,
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
            _togglesMock.Object,
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
            _togglesMock.Object,
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
            _togglesMock.Object,
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
            _togglesMock.Object,
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
            _togglesMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
            () => executor.ShouldExecuteNow());

        Assert.Equal("DayOfWeek", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUseSubscriberServiceToggleIsEnabled_CallsSubscriberService()
    {
        // Arrange
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Daily,
            HourOfDay = 7
        };

        // Set up feature toggle to be enabled
        var featureToggles = new FeatureTogglesOptions
        {
            Toggles = new List<FeatureToggle>
            {
                new FeatureToggle { Name = "UseSubscriberService", State = true }
            }
        };
        _togglesMock.Setup(x => x.CurrentValue).Returns(featureToggles);

        // Set up mocks for dependencies
        var subscriptions = Array.Empty<Subscriber>();
        _subscriberServiceMock
            .Setup(x => x.GetSubscriptionsAsync((int)NewsletterIntervals.Daily))
            .ReturnsAsync(subscriptions);

        var articles = Array.Empty<Article>();
        _articleServiceMock
            .Setup(x => x.GetArticlesSinceAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(articles);

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _togglesMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act
        await executor.ExecuteAsync(CancellationToken.None);

        // Assert
        _subscriberServiceMock.Verify(
            x => x.GetSubscriptionsAsync((int)NewsletterIntervals.Daily),
            Times.Once);
        _articleServiceMock.Verify(
            x => x.GetArticlesSinceAsync(It.IsAny<DateTimeOffset>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUseSubscriberServiceToggleIsDisabled_DoesNotCallSubscriberService()
    {
        // Arrange
        var options = new IntervalOptions
        {
            Interval = NewsletterIntervals.Daily,
            HourOfDay = 7
        };

        // Set up feature toggle to be disabled
        var featureToggles = new FeatureTogglesOptions
        {
            Toggles = new List<FeatureToggle>
            {
                new FeatureToggle { Name = "UseSubscriberService", State = false }
            }
        };
        _togglesMock.Setup(x => x.CurrentValue).Returns(featureToggles);

        var executor = new NewsletterExecutor(
            options,
            _loggerMock.Object,
            _fakeTimeProvider,
            _subscriberServiceMock.Object,
            _togglesMock.Object,
            _emailServiceMock.Object,
            _emailFactoryMock.Object,
            _articleServiceMock.Object);

        // Act
        await executor.ExecuteAsync(CancellationToken.None);

        // Assert
        _subscriberServiceMock.Verify(
            x => x.GetSubscriptionsAsync(It.IsAny<int>()),
            Times.Never);
        _articleServiceMock.Verify(
            x => x.GetArticlesSinceAsync(It.IsAny<DateTimeOffset>()),
            Times.Never);
        _emailServiceMock.Verify(
            x => x.SendEmails(It.IsAny<IEnumerable<Email>>()),
            Times.Never);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("SubscriberService is turned off")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
