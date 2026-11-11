using Microsoft.AspNetCore.Components;
using JournalistPages.web.Models;

namespace JournalistPages.web.Pages.Articles;

public partial class ArticlePresenter : ComponentBase
{
    [Parameter]
    public required Article Article { get; set; }
}
