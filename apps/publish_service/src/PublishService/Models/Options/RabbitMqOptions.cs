namespace PublishService.Models.Options;

public sealed class RabbitMqOptions
{
    public required string HostName { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
}
