namespace NewsletterService.Shared.Options;

public class NewsletterOptions
{
    public List<IntervalOptions>? Intervals { get; set; }
}

public class IntervalOptions
{
    public required NewsletterIntervals Interval { get; init; }

    public DayOfWeek? DayOfWeek { get; init; }

    public required int HourOfDay { get; init; }
}