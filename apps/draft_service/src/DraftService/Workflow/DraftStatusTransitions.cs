using DraftService.Models;

namespace DraftService.Workflow;

public enum DraftAction
{
    SubmitForApproval,
    Approve,
    Reject,
    Publish,
    Archive
}

// Single source of truth for which status transitions are legal, so the controller
// never hardcodes the workflow inline and the tests exercise the same rules it uses.
public static class DraftStatusTransitions
{
    private static readonly Dictionary<DraftAction, (DraftStatus From, DraftStatus To)[]> Rules = new()
    {
        [DraftAction.SubmitForApproval] = [(DraftStatus.WorkInProgress, DraftStatus.PendingApproval)],
        [DraftAction.Approve] = [(DraftStatus.PendingApproval, DraftStatus.Approved)],
        [DraftAction.Reject] = [(DraftStatus.PendingApproval, DraftStatus.WorkInProgress)],
        [DraftAction.Publish] = [(DraftStatus.Approved, DraftStatus.Published)],
        [DraftAction.Archive] =
        [
            (DraftStatus.WorkInProgress, DraftStatus.Archived),
            (DraftStatus.PendingApproval, DraftStatus.Archived),
            (DraftStatus.Approved, DraftStatus.Archived)
        ]
    };

    public static bool TryGetResultStatus(DraftAction action, DraftStatus currentStatus, out DraftStatus resultStatus)
    {
        foreach (var rule in Rules[action])
        {
            if (rule.From == currentStatus)
            {
                resultStatus = rule.To;
                return true;
            }
        }

        resultStatus = currentStatus;
        return false;
    }

    public static bool CanEditContent(DraftStatus status) => status == DraftStatus.WorkInProgress;
}
