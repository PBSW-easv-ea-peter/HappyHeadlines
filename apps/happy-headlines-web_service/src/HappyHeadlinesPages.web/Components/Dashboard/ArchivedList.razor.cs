using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace HappyHeadlinesPages.web.Components.Dashboard;

public partial class ArchivedList : ComponentBase
{
    [Parameter]
    public IEnumerable<Draft> Drafts { get; set; } = [];

    [Parameter]
    public EventCallback<long> OnReactivate { get; set; }
}
