using Microsoft.AspNetCore.Components;
using ReaderPages.web.Models;

namespace ReaderPages.web.Components.Dashboard;

public partial class PublishedList : ComponentBase
{
    [Parameter]
    public IEnumerable<ArticleDTO> Articles { get; set; } = [];
}
