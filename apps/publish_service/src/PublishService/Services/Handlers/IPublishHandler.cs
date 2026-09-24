namespace PublishService.Services.Handlers;

public interface IPublishHandler
{
    Task<IResult> PublishAsync(Guid id);
}