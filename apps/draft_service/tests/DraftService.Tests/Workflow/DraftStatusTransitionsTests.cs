using DraftService.Models;
using DraftService.Workflow;
using Xunit;

namespace DraftService.Tests.Workflow;

public class DraftStatusTransitionsTests
{
    [Theory]
    [InlineData(DraftAction.SubmitForApproval, DraftStatus.WorkInProgress, DraftStatus.PendingApproval)]
    [InlineData(DraftAction.Approve, DraftStatus.PendingApproval, DraftStatus.Approved)]
    [InlineData(DraftAction.Reject, DraftStatus.PendingApproval, DraftStatus.WorkInProgress)]
    [InlineData(DraftAction.Publish, DraftStatus.Approved, DraftStatus.Published)]
    [InlineData(DraftAction.Archive, DraftStatus.WorkInProgress, DraftStatus.Archived)]
    [InlineData(DraftAction.Archive, DraftStatus.PendingApproval, DraftStatus.Archived)]
    [InlineData(DraftAction.Archive, DraftStatus.Approved, DraftStatus.Archived)]
    public void TryGetResultStatus_LegalTransition_ReturnsTrueWithExpectedStatus(
        DraftAction action, DraftStatus from, DraftStatus expectedTo)
    {
        var isLegal = DraftStatusTransitions.TryGetResultStatus(action, from, out var resultStatus);

        Assert.True(isLegal);
        Assert.Equal(expectedTo, resultStatus);
    }

    [Theory]
    [InlineData(DraftAction.SubmitForApproval, DraftStatus.PendingApproval)]
    [InlineData(DraftAction.SubmitForApproval, DraftStatus.Published)]
    [InlineData(DraftAction.Approve, DraftStatus.WorkInProgress)]
    [InlineData(DraftAction.Approve, DraftStatus.Approved)]
    [InlineData(DraftAction.Reject, DraftStatus.WorkInProgress)]
    [InlineData(DraftAction.Publish, DraftStatus.WorkInProgress)]
    [InlineData(DraftAction.Publish, DraftStatus.PendingApproval)]
    [InlineData(DraftAction.Archive, DraftStatus.Published)]
    [InlineData(DraftAction.Archive, DraftStatus.Archived)]
    public void TryGetResultStatus_IllegalTransition_ReturnsFalseAndLeavesStatusUnchanged(
        DraftAction action, DraftStatus from)
    {
        var isLegal = DraftStatusTransitions.TryGetResultStatus(action, from, out var resultStatus);

        Assert.False(isLegal);
        Assert.Equal(from, resultStatus);
    }

    [Theory]
    [InlineData(DraftStatus.WorkInProgress, true)]
    [InlineData(DraftStatus.PendingApproval, false)]
    [InlineData(DraftStatus.Approved, false)]
    [InlineData(DraftStatus.Published, false)]
    [InlineData(DraftStatus.Archived, false)]
    public void CanEditContent_OnlyTrueForWorkInProgress(DraftStatus status, bool expected)
    {
        Assert.Equal(expected, DraftStatusTransitions.CanEditContent(status));
    }
}
