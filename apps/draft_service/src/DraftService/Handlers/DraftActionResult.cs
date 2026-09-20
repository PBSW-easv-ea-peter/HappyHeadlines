using DraftService.Models;

namespace DraftService.Handlers;

public enum DraftActionOutcome
{
    Success,
    NotFound,
    ValidationFailed,
    IllegalTransition,
    ConcurrentChange
}

// Carries a use-case outcome without depending on ASP.NET Core's ActionResult - mapping an
// outcome to an HTTP status code is DraftsController's job, not the handler's.
public record DraftActionResult(DraftActionOutcome Outcome, Draft? Draft, string? Message = null)
{
    public static DraftActionResult Success(Draft draft) => new(DraftActionOutcome.Success, draft);

    public static DraftActionResult NotFound() => new(DraftActionOutcome.NotFound, null);

    public static DraftActionResult ValidationFailed(string message) =>
        new(DraftActionOutcome.ValidationFailed, null, message);

    public static DraftActionResult IllegalTransition(string message) =>
        new(DraftActionOutcome.IllegalTransition, null, message);

    public static DraftActionResult ConcurrentChange() =>
        new(DraftActionOutcome.ConcurrentChange, null, "Draft status changed concurrently - please retry.");
}
