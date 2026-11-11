using JournalistPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace JournalistPages.web.Components.Dashboard;

public partial class ArchivedList : ComponentBase
{
    [Parameter]
    public IEnumerable<Draft> Drafts { get; set; } = [];

    [Parameter]
    public EventCallback<Guid> OnReactivate { get; set; }
}
