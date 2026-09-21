using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace HappyHeadlinesPages.web.Components.Dashboard;

public partial class PublishedList : ComponentBase
{
    [Parameter]
    public IEnumerable<ArticleDTO> Articles { get; set; } = [];
}
