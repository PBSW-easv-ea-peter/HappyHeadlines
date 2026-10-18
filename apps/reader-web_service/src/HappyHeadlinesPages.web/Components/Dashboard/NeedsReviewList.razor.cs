using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace HappyHeadlinesPages.web.Components.Dashboard;

public partial class NeedsReviewList : ComponentBase
{
    [Parameter]
    public IEnumerable<Draft> Drafts { get; set; } = [];
}
