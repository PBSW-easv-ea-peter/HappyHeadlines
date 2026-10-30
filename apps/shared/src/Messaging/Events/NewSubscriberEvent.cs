namespace Messaging.Events;

public class NewSubscriberEvent
{
    public required Guid SubscriberId { get; init; }

    public required int[] Intervals { get; init; }
}
