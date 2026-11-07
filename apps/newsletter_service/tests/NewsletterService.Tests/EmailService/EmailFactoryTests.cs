using NewsletterService.NewsletterDistributors.EmailService;
using NewsletterService.Services.ArticleService;
using NewsletterService.Services.SubscriberService;

namespace NewsletterService.Tests.EmailService;

public class EmailFactoryTests
{
    private readonly EmailFactory _emailFactory;

    public EmailFactoryTests()
    {
        _emailFactory = new EmailFactory();
    }

    #region CreateDailyNewsletterAsync Tests
    [Fact]
    public async Task CreateDailyNewsletterAsync_ReturnsCorrectEmail()
    {
        // Arrange
        var subscriber = new Subscriber
        {
            Email = "test@example.com",
            Username = "TestUser",
            Interests = []
        };

        var articles = new List<Article>
        {
            new Article
            {
                Title = "Test Article 1",
                Byline = "By Author 1",
                BreadText = "This is the content of article 1. It should be truncated if too long.",
                PublishDate = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Location = 1,
                SectionName = "News"
            }
        };

        // Act
        var result = await _emailFactory.CreateDailyNewsletterAsync(subscriber, articles);

        // Assert
        Assert.Equal(subscriber.Email, result.EmailAddress);
        Assert.Equal(subscriber.Username, result.NameOfReceiver);
        Assert.Equal("Your Daily Happy Headlines", result.Subject);
        Assert.Contains("Test Article 1", result.Content);
        Assert.Contains("By Author 1", result.Content);
        Assert.Contains("News", result.Content);
        Assert.Contains("2024-01-01 10:00", result.Content);
    }

    [Fact]
    public async Task CreateDailyNewsletterAsync_FiltersArticlesBySectionAndRegion()
    {
        // Arrange - Subscriber interested in (News, Region1) and (Sports, Region1)
        var subscriber = new Subscriber
        {
            Email = "test@example.com",
            Username = "TestUser",
            Interests = new[]
            {
                new Interest { Section = "News", Region = 1 },
                new Interest { Section = "Sports", Region = 1 }
            }
        };

        var articles = new List<Article>
        {
            // This matches (News, Region1) - should be included
            new Article
            {
                Title = "News Region 1 Article",
                Byline = "By Author 1",
                BreadText = "Content for news region 1",
                PublishDate = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero),
                Location = 1,
                SectionName = "News"
            },
            // This matches (Sports, Region1) - should be included
            new Article
            {
                Title = "Sports Region 1 Article",
                Byline = "By Author 2",
                BreadText = "Content for sports region 1",
                PublishDate = new DateTimeOffset(2024, 1, 2, 10, 0, 0, TimeSpan.Zero),
                Location = 1,
                SectionName = "Sports"
            },
            // This is (Entertainment, Region1) - NOT in interests, should NOT be included
            new Article
            {
                Title = "Entertainment Region 1 Article",
                Byline = "By Author 3",
                BreadText = "Content for entertainment region 1",
                PublishDate = new DateTimeOffset(2024, 1, 3, 10, 0, 0, TimeSpan.Zero),
                Location = 1,
                SectionName = "Entertainment"
            },
            // This is (News, Region2) - NOT in interests, should NOT be included
            new Article
            {
                Title = "News Region 2 Article",
                Byline = "By Author 4",
                BreadText = "Content for news region 2",
                PublishDate = new DateTimeOffset(2024, 1, 4, 10, 0, 0, TimeSpan.Zero),
                Location = 2,
                SectionName = "News"
            }
        };

        // Act
        var result = await _emailFactory.CreateDailyNewsletterAsync(subscriber, articles);

        // Assert
        Assert.Equal(subscriber.Email, result.EmailAddress);
        Assert.Equal(subscriber.Username, result.NameOfReceiver);
        
        // Should contain articles that match (News, Region1) and (Sports, Region1)
        Assert.Contains("News Region 1 Article", result.Content);
        Assert.Contains("Sports Region 1 Article", result.Content);
        
        // Should NOT contain articles that don't match the matrix
        Assert.DoesNotContain("Entertainment Region 1 Article", result.Content);
        Assert.DoesNotContain("News Region 2 Article", result.Content);
    }

    [Fact]
    public async Task CreateDailyNewsletterAsync_FiltersArticlesByMatrix()
    {
        // Arrange - Subscriber interested in (News, Global) and (Sports, Europe)
        var subscriber = new Subscriber
        {
            Email = "test@example.com",
            Username = "TestUser",
            Interests = new[]
            {
                new Interest { Section = "News", Region = 0 },  // Global = 0
                new Interest { Section = "Sports", Region = 100 } // Europe = 100
            }
        };

        var articles = new List<Article>
        {
            // Matches (News, Global) - should be included
            new Article
            {
                Title = "Global News Article",
                Byline = "By Author 1",
                BreadText = "Content for global news",
                PublishDate = DateTimeOffset.Now,
                Location = 0,
                SectionName = "News"
            },
            // Matches (Sports, Europe) - should be included
            new Article
            {
                Title = "Europe Sports Article",
                Byline = "By Author 2",
                BreadText = "Content for europe sports",
                PublishDate = DateTimeOffset.Now,
                Location = 100,
                SectionName = "Sports"
            },
            // (News, Europe) - NOT in interests, should NOT be included
            new Article
            {
                Title = "Europe News Article",
                Byline = "By Author 3",
                BreadText = "Content for europe news",
                PublishDate = DateTimeOffset.Now,
                Location = 100,
                SectionName = "News"
            },
            // (Sports, Global) - NOT in interests, should NOT be included
            new Article
            {
                Title = "Global Sports Article",
                Byline = "By Author 4",
                BreadText = "Content for global sports",
                PublishDate = DateTimeOffset.Now,
                Location = 0,
                SectionName = "Sports"
            }
        };

        // Act
        var result = await _emailFactory.CreateDailyNewsletterAsync(subscriber, articles);

        // Assert
        // Should contain articles that match the matrix
        Assert.Contains("Global News Article", result.Content);
        Assert.Contains("Europe Sports Article", result.Content);
        
        // Should NOT contain articles that don't match the matrix
        Assert.DoesNotContain("Europe News Article", result.Content);
        Assert.DoesNotContain("Global Sports Article", result.Content);
    }

    [Fact]
    public async Task CreateDailyNewsletterAsync_HandlesEmptyArticles()
    {
        // Arrange
        var subscriber = new Subscriber
        {
            Email = "test@example.com",
            Username = "TestUser",
            Interests = []
        };

        var articles = new List<Article>();

        // Act
        var result = await _emailFactory.CreateDailyNewsletterAsync(subscriber, articles);

        // Assert
        Assert.Equal(subscriber.Email, result.EmailAddress);
        Assert.Equal(subscriber.Username, result.NameOfReceiver);
        Assert.Equal("Your Daily Happy Headlines", result.Subject);
    }
    #endregion

    #region CreateWeeklyNewsletterAsync Tests
    
    [Fact]
    public async Task CreateWeeklyNewsletterAsync_FiltersArticlesBySectionAndRegion()
    {
        // Arrange - Subscriber interested in (Politics, NorthAmerica)
        var subscriber = new Subscriber
        {
            Email = "test@example.com",
            Username = "TestUser",
            Interests = new[]
            {
                new Interest { Section = "Politics", Region = 500 } // NorthAmerica = 500
            }
        };

        var articles = new List<Article>
        {
            // Matches (Politics, NorthAmerica) - should be included
            new Article
            {
                Title = "North America Politics Article",
                Byline = "By Author 1",
                BreadText = "Content",
                PublishDate = DateTimeOffset.Now,
                Location = 500,
                SectionName = "Politics"
            },
            // (Sports, NorthAmerica) - NOT in interests, should NOT be included
            new Article
            {
                Title = "North America Sports Article",
                Byline = "By Author 2",
                BreadText = "Content",
                PublishDate = DateTimeOffset.Now,
                Location = 500,
                SectionName = "Sports"
            },
            // (Politics, Europe) - NOT in interests, should NOT be included
            new Article
            {
                Title = "Europe Politics Article",
                Byline = "By Author 3",
                BreadText = "Content",
                PublishDate = DateTimeOffset.Now,
                Location = 100,
                SectionName = "Politics"
            }
        };

        // Act
        var result = await _emailFactory.CreateWeeklyNewsletterAsync(subscriber, articles);

        // Assert
        Assert.Contains("North America Politics Article", result.Content);
        Assert.DoesNotContain("North America Sports Article", result.Content);
        Assert.DoesNotContain("Europe Politics Article", result.Content);
    }

    #endregion
}
