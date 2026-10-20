using Microsoft.AspNetCore.Components;
using ReaderPages.web.Models;

namespace ReaderPages.web.Components.Dashboard;

public partial class YourWorkList : ComponentBase
{
    [Parameter]
    public IEnumerable<Draft> Drafts { get; set; } = [];
}
