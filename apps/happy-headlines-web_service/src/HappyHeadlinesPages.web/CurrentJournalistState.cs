using HappyHeadlinesPages.web.Models;

namespace HappyHeadlinesPages.web;

// Placeholder for a real login system - lives in DI so the "Signed in as"
// picker in the nav and the Drafts page always agree on who's selected.
public class CurrentJournalistState
{
    public long JournalistId { get; private set; } = Journalists.All[0].Id;

    public event Action? OnChange;

    public void SetJournalist(long journalistId)
    {
        if (JournalistId == journalistId)
            return;

        JournalistId = journalistId;
        OnChange?.Invoke();
    }
}
