using Microsoft.AspNetCore.Components;
using HappyHeadlinesApp.Models;

namespace HappyHeadlinesApp.Pages.Articles;

public partial class ArticlePresenter : ComponentBase
{
    [Parameter]
    public required Article Article { get; set; }
}
