
namespace PublishService.Endpoints;

public static class PublishActions
{
    public static void MapPublishActionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("publish-draft/{id:int}", PublishDraft);
    }

    private static IResult PublishDraft()
    {
        return Results.Ok();
    }
}
