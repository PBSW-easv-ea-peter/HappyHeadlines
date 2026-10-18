using JournalistPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace JournalistPages.web.Components.Dashboard;

public partial class PublishedList : ComponentBase
{
    [Parameter]
    public IEnumerable<ArticleDTO> Articles { get; set; } = [];
}
