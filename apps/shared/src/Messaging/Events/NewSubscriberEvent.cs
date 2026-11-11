namespace Messaging.Events;

public class NewSubscriberEvent
{
    public required string Username { get; init; }

    public required string Email { get; init; }
}
