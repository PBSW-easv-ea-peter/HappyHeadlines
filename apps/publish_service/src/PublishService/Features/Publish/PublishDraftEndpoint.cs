using Microsoft.AspNetCore.Mvc;
using PublishService.Shared;
using PublishService.Shared.Exceptions;

namespace PublishService.Features.Publish;

public class PublishDraftEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/");

        group.MapPost("publish-draft/{id:guid}", PublishDraft);
    }

    private static async Task<IResult> PublishDraft(
        [FromServices] IPublishDraftHandler handler,
        Guid id)
    {
        try
        {
            return await handler.PublishAsync(id);
        }
        catch (InfrastructureException e)
        {
            return Results.Problem(e.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}