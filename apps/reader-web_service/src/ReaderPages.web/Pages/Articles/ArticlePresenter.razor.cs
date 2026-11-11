using Microsoft.AspNetCore.Components;
using ReaderPages.web.Models;

namespace ReaderPages.web.Pages.Articles;

public partial class ArticlePresenter : ComponentBase
{
    [Parameter]
    public required Article Article { get; set; }
}
