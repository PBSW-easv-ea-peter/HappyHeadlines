namespace PublishService.Shared;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}