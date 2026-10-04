using HappyHeadlinesPages.web.Models;

namespace HappyHeadlines.reader.wasm;

public static class ArticleDisplay
{
    // Seeded articles may not have a PublishDate yet, so fall back to CreatedDate.
    public static DateTimeOffset PublishedAt(ArticleDTO article) =>
        article.PublishDate ?? article.CreatedDate;

    // Relative, so it resolves against <base href="/">. The region is the
    // article's own shard, since ids are only unique within one.
    public static string Href(ArticleDTO article) =>
        $"article/{article.Id}?region={article.Location}";

    public static string FormatDate(ArticleDTO article) =>
        PublishedAt(article).ToLocalTime().ToString("d MMM yyyy");

    public static string Excerpt(string text, int maxLength = 240)
    {
        if (text.Length <= maxLength)
            return text;

        var cut = text.LastIndexOf(' ', maxLength);

        return text[..(cut > 0 ? cut : maxLength)].TrimEnd(',', '.', ' ') + "…";
    }
}
