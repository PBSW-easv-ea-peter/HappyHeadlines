using Microsoft.AspNetCore.Components;
using ReaderPages.web.Models;

namespace ReaderPages.web.Components.Dashboard;

public partial class ArchivedList : ComponentBase
{
    [Parameter]
    public IEnumerable<Draft> Drafts { get; set; } = [];

    [Parameter]
    public EventCallback<Guid> OnReactivate { get; set; }
}
