using JournalistPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace JournalistPages.web.Components.Dashboard;

public partial class NeedsReviewList : ComponentBase
{
    [Parameter]
    public IEnumerable<Draft> Drafts { get; set; } = [];
}
