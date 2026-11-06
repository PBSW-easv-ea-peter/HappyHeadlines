namespace Messaging.Events;

public class NewSubscriberEvent
{
    public required string Username { get; init; }

    public required string Email { get; init; }

    public required int[] Intervals { get; init; }

    public required int[] SectionIds { get; init; }
    
    public required int[] RegionIds { get; init; }
}
