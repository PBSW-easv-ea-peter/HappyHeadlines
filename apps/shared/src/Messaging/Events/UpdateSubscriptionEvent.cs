namespace Messaging.Events;

public class UpdateSubscriptionEvent
{
    public required Guid SubscriberId { get; init; }

    // If left empty all subscriptions are removed.
    public required int[] Intervals { get; init; }
}
