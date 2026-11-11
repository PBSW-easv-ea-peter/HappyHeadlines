using NewsletterService.Services.ArticleService;
using NewsletterService.Services.SubscriberService;

namespace NewsletterService.NewsletterDistributors.EmailService;

public interface IEmailFactory
{
    public Email CreateWelcomeEmail(string email, string nameOfReceiver);
    public Task<Email> CreateDailyNewsletterAsync(Subscriber subscriber, IReadOnlyCollection<Article> articles);
    public Task<Email> CreateWeeklyNewsletterAsync(Subscriber subscriber, IReadOnlyCollection<Article> articles);
}

public class EmailFactory : IEmailFactory
{
    private const int BreadTextPreviewLength = 150;

    private const string NewsletterTemplate = """
                                              <!DOCTYPE html>
                                              <html>
                                              <head>
                                                  <style>
                                                      body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; padding: 20px; }}
                                                      .header {{ background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin-bottom: 20px; }}
                                                      .header h1 {{ color: #2c3e50; margin: 0; }}
                                                      .article {{ background-color: #fff; border: 1px solid #e0e0e0; border-radius: 8px; padding: 15px; margin-bottom: 20px; }}
                                                      .article-title {{ color: #2c3e50; font-size: 1.4em; margin-top: 0; margin-bottom: 10px; }}
                                                      .article-meta {{ color: #666; font-size: 0.9em; margin-bottom: 10px; }}
                                                      .article-byline {{ font-style: italic; color: #555; margin-bottom: 10px; }}
                                                      .article-preview {{ color: #444; }}
                                                      .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; color: #666; font-size: 0.9em; }}
                                                  </style>
                                              </head>
                                              <body>
                                                  <div class='header'>
                                                      <h1>Happy Headlines - {0} Newsletter</h1>
                                                      <p>Hello {1}, here are your latest news articles!</p>
                                                  </div>
                                                  
                                                  {2}
                                                  
                                                  <div class='footer'>
                                                      <p>Thank you for reading Happy Headlines!</p>
                                                      <p>Stay happy and informed!</p>
                                                  </div>
                                              </body>
                                              </html>
                                              """;

    private const string ArticleTemplate = """
                                           <div class='article'>
                                               <h2 class='article-title'>{0}</h2>
                                               <div class='article-meta'>
                                                   <strong>Section:</strong> {1} | 
                                                   <strong>Region:</strong> {2} | 
                                                   <strong>Published:</strong> {3}
                                               </div>
                                               <div class='article-byline'>{4}</div>
                                               <div class='article-preview'>{5}</div>
                                           </div>
                                           """;

    public Email CreateWelcomeEmail(string email, string nameOfReceiver)
    {
        return new Email
        {
            EmailAddress = email,
            NameOfReceiver = nameOfReceiver,
            Subject = "Welcome to Happy Headlines",
            Content = $"We're happy to have you onboard our newsletter {nameOfReceiver}!"
        };
    }

    public Task<Email> CreateDailyNewsletterAsync(Subscriber subscriber, IReadOnlyCollection<Article> articles)
    {
        var filteredArticles = FilterArticlesByInterests(subscriber, articles);
        var emailContent = BuildHtmlNewsletter(subscriber.Username, filteredArticles, "Daily");
        
        Email email = new()
        {
            EmailAddress = subscriber.Email,
            NameOfReceiver = subscriber.Username,
            Subject = "Your Daily Happy Headlines",
            Content = emailContent
        };
        
        return Task.FromResult(email);
    }
    
    public Task<Email> CreateWeeklyNewsletterAsync(Subscriber subscriber, IReadOnlyCollection<Article> articles)
    {
        var filteredArticles = FilterArticlesByInterests(subscriber, articles);
        var emailContent = BuildHtmlNewsletter(subscriber.Username, filteredArticles, "Weekly");
        
        Email email = new()
        {
            EmailAddress = subscriber.Email,
            NameOfReceiver = subscriber.Username,
            Subject = "Your Weekly Happy Headlines",
            Content = emailContent
        };
        
        return Task.FromResult(email);
    }

    private IReadOnlyCollection<Article> FilterArticlesByInterests(Subscriber subscriber, IReadOnlyCollection<Article> articles)
    {
        if (subscriber.Interests.Length == 0)
            return articles;
        
        // Create a set of (Section, Region) tuples that the subscriber is interested in
        var interestMatrix = subscriber.Interests
            .Select(i => (i.Section, i.Region))
            .ToHashSet();
        
        // Filter articles: an article must match BOTH Section AND Region from at least one interest
        return articles.Where(a => 
            interestMatrix.Contains((a.SectionName, a.Location)))
            .ToList();
    }

    private string BuildHtmlNewsletter(string username, IReadOnlyCollection<Article> articles, string newsletterType)
    {
        var articlesHtml = string.Join("\n", articles.Select(FormatArticleHtml));
        
        return string.Format(NewsletterTemplate, newsletterType, username, articlesHtml);
    }

    private string FormatArticleHtml(Article article)
    {
        var breadTextPreview = TruncateBreadText(article.BreadText);
        var publishedDate = article.PublishDate.ToString("yyyy-MM-dd HH:mm");
        
        return string.Format(ArticleTemplate, 
            article.Title,
            article.SectionName,
            article.Location,
            publishedDate,
            article.Byline,
            breadTextPreview);
    }

    private static string TruncateBreadText(string breadText)
    {
        if (string.IsNullOrEmpty(breadText))
            return string.Empty;
        
        if (breadText.Length <= BreadTextPreviewLength)
            return breadText;
        
        return breadText[..BreadTextPreviewLength] + "...";
    }
}
