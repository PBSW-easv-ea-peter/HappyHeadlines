using Microsoft.AspNetCore.Components;
using HappyHeadlinesPages.web.Models;

namespace HappyHeadlinesPages.web.Pages.Articles;

public partial class ArticlePresenter : ComponentBase
{
    [Parameter]
    public required Article Article { get; set; }
}
