using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NewsletterService.Intervals;
using NewsletterService.Shared.Options;
using Microsoft.Extensions.Time.Testing;

namespace NewsletterService.Tests.Intervals;

public class DailyNewsletterServiceTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly DailyNewsletterService _service;
    private readonly Mock<ILogger<DailyNewsletterService>> _logger = new();

    public DailyNewsletterServiceTests()
    {
        // Set up a fake time provider starting at a known date/time
        // Using November 6, 2026 at 10:00:00 AM
        _fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 11, 6, 10, 0, 0, TimeSpan.Zero));

        var options = Options.Create(new IntervalOptions
        {
            IntervalName = "Daily",
            HourOfDay = 9  // Target 9 AM daily
        });

        _service = new DailyNewsletterService(
            options,
            _logger.Object,
            _fakeTimeProvider);
    }

    [Fact]
    public async Task Delay_ShouldWaitUntilNextDayAtConfiguredHour()
    {
        // Arrange
        var delayTask = _service.Delay(CancellationToken.None);

        // Assert - Task should not have completed yet
        Assert.False(delayTask.IsCompleted);

        // Act - Advance time by 22 hours
        _fakeTimeProvider.Advance(TimeSpan.FromHours(22));

        // Assert - Still waiting
        Assert.False(delayTask.IsCompleted);

        // Act - Advance the final hour
        _fakeTimeProvider.Advance(TimeSpan.FromHours(1));

        // Assert - Delay should now be completed
        await delayTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(delayTask.IsCompletedSuccessfully);
    }
}
