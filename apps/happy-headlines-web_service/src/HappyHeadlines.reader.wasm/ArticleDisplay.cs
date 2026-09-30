using HappyHeadlinesPages.web.Models;

namespace HappyHeadlines.reader.wasm;

public static class ArticleDisplay
{
    // Seeded articles may not have a PublishDate yet, so fall back to CreatedDate.
    public static DateTimeOffset PublishedAt(ArticleDTO article) =>
        article.PublishDate ?? article.CreatedDate;

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
