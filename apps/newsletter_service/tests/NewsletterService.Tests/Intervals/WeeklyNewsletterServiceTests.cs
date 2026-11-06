using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NewsletterService.Intervals;
using NewsletterService.Shared.Options;
using Microsoft.Extensions.Time.Testing;

namespace NewsletterService.Tests.Intervals;

public class WeeklyNewsletterServiceTests
{
    private readonly FakeTimeProvider _fakeTimeProvider;
    private readonly WeeklyNewsletterService _service;
    private readonly Mock<ILogger<WeeklyNewsletterService>> _logger = new();

    public WeeklyNewsletterServiceTests()
    {
        // Friday, November 6, 2026, at 10:00
        _fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 11, 6, 10, 0, 0, TimeSpan.Zero));

        var options = Options.Create(new IntervalOptions
        {
            IntervalName = "Weekly",
            DayOfWeek = DayOfWeek.Saturday,
            HourOfDay = 8
        });

        _service = new WeeklyNewsletterService(
            options,
            _logger.Object,
            _fakeTimeProvider);
    }

    [Fact]
    public async Task Delay_ShouldWaitUntilNextSaturdayAtConfiguredHour()
    {
        // Arrange
        var delayTask = _service.Delay(CancellationToken.None);

        // Assert - Still waiting
        Assert.False(delayTask.IsCompleted);

        // Advance 21 hours (Friday 10 AM -> Saturday 7 AM)
        _fakeTimeProvider.Advance(TimeSpan.FromHours(21));

        Assert.False(delayTask.IsCompleted);

        // Advance one more hour (Saturday 8 AM)
        _fakeTimeProvider.Advance(TimeSpan.FromHours(1));

        await delayTask;

        Assert.True(delayTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Delay_ShouldWaitAtConfiguredHour()
    {
        // Arrange - Saturday, November 7, 2026, at 7:00 AM
        _fakeTimeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);
        _fakeTimeProvider.Advance(TimeSpan.FromHours(21));

        var delayTask = _service.Delay(CancellationToken.None);

        // Assert - Still waiting
        Assert.False(delayTask.IsCompleted);

        // Act - Advance 59 minutes
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(59));

        Assert.False(delayTask.IsCompleted);

        // Advance the final minute
        _fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));

        await delayTask;

        Assert.True(delayTask.IsCompletedSuccessfully);
    }

    [Fact]
    public void Delay_WhenPastTargetTimeThisWeek_WaitsUntilNextWeek()
    {
        // Arrange - Set time to Friday 10:00 AM (past the 9:00 AM target)
        _fakeTimeProvider.SetUtcNow(new DateTimeOffset(2026, 11, 7, 10, 0, 0, TimeSpan.Zero));
        
        var options = Options.Create(new IntervalOptions
        {
            IntervalName = "Weekly",
            DayOfWeek = DayOfWeek.Friday,
            HourOfDay = 9
        });
        
        var service = new WeeklyNewsletterService(
            options,
            _logger.Object,
            _fakeTimeProvider);

        // Act
        var delayTask = service.Delay(CancellationToken.None);
        
        // Assert
        Assert.NotNull(delayTask);
        Assert.False(delayTask.IsCompleted);
    }

    [Fact]
    public async Task Delay_WhenBeforeTargetTime_WaitsUntilSameWeek()
    {
        // Arrange - Friday, November 6, 2026, at 8:00 AM
        // Target: Friday at 9:00 AM
        var fakeTimeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 11, 6, 8, 0, 0, TimeSpan.Zero));

        var options = Options.Create(new IntervalOptions
        {
            IntervalName = "Weekly",
            DayOfWeek = DayOfWeek.Friday,
            HourOfDay = 9
        });

        var service = new WeeklyNewsletterService(
            options,
            _logger.Object,
            fakeTimeProvider);

        // Act
        var delayTask = service.Delay(CancellationToken.None);

        // Assert - Still waiting
        Assert.False(delayTask.IsCompleted);

        // Advance 59 minutes
        fakeTimeProvider.Advance(TimeSpan.FromMinutes(59));

        Assert.False(delayTask.IsCompleted);

        // Advance the final minute
        fakeTimeProvider.Advance(TimeSpan.FromMinutes(1));

        await delayTask;

        Assert.True(delayTask.IsCompletedSuccessfully);
    }
}
