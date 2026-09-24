using Microsoft.AspNetCore.Mvc;
using PublishService.Services.Handlers;

namespace PublishService.Endpoints;

public static class PublishActions
{
    public static void MapPublishActionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("publish-draft/{id:guid}", PublishDraft);
    }

    private static async Task<IResult> PublishDraft(
        [FromServices] IPublishHandler handler,
        Guid id)
    {
        return await handler.PublishAsync(id);
    }
}