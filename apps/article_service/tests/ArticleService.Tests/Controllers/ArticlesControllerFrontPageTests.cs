using ArticleService.Cache;
using ArticleService.Controllers;
using ArticleService.Models;
using ArticleService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArticleService.Tests.Controllers;

// GET /api/articles/{location}/frontpage (GAF): only what the page shows, cached as a whole.
public class ArticlesControllerFrontPageTests
{
    private readonly Mock<IArticleReadRepository> _readRepository = new();
    private readonly Mock<IArticleCache> _cache = new();
    private readonly Mock<ILogger<ArticlesController>> _logger = new();
    private readonly Mock<IOptionsMonitor<LocationConfig>> _locations = new();
    private readonly ArticlesController _controller;

    public ArticlesControllerFrontPageTests()
    {
        _readRepository.Setup(r => r.GetFrontPageAsync(It.IsAny<string>(), It.IsAny<FrontPageQuery>()))
            .ReturnsAsync([new Article { Title = "From database" }]);

        _locations.Setup(l => l.CurrentValue).Returns(new LocationConfig
        {
            Locations = [
                new Location { Code = "EU", Description = "Europe", Enabled = true },
                new Location { Code = "GO", Description = "Global", Enabled = true },
                new Location { Code = "AN", Description = "Antarctica", Enabled = true }
            ]
        });

        _controller = new ArticlesController(
            _readRepository.Object, 
            Mock.Of<IArticleWriteRepository>(), 
            _cache.Object, 
            _locations.Object,
            _logger.Object);
    }

    private static IEnumerable<string> Titles(ActionResult<IEnumerable<Article>> result) =>
        ((IEnumerable<Article>)Assert.IsType<OkObjectResult>(result.Result).Value!).Select(a => a.Title);

    [Fact]
    public async Task PassesTheClientsLayoutToTheQuery()
    {
        await _controller.GetFrontPage("EU", top: 6, perSection: 3, section: "Health", excerpt: 0, heroExcerpt: 400);

        _readRepository.Verify(r => r.GetFrontPageAsync("EU", new FrontPageQuery(6, 3, "Health", 0, 400)));
    }

    [Fact]
    public async Task CacheMiss_ReadsTheDatabaseAndStoresTheResponse()
    {
        var result = await _controller.GetFrontPage("EU", top: 6, perSection: 3);

        Assert.Equal(["From database"], Titles(result));
        _cache.Verify(c => c.StoreFrontPageAsync("EU", new FrontPageQuery(6, 3, null, null, null),
            It.Is<IList<Article>>(a => a.Single().Title == "From database")));
    }

    [Fact]
    public async Task CacheHit_DoesNotTouchTheDatabase()
    {
        _cache.Setup(c => c.GetFrontPageAsync("GO", It.IsAny<FrontPageQuery>()))
            .ReturnsAsync([new Article { Title = "Cached" }]);

        var result = await _controller.GetFrontPage("GO", top: 6, perSection: 3);

        Assert.Equal(["Cached"], Titles(result));
        _readRepository.Verify(r => r.GetFrontPageAsync(It.IsAny<string>(), It.IsAny<FrontPageQuery>()), Times.Never);
    }

    [Fact]
    public async Task CachedEmptyFrontPage_IsAHit()
    {
        _cache.Setup(c => c.GetFrontPageAsync("AN", It.IsAny<FrontPageQuery>())).ReturnsAsync([]);

        await _controller.GetFrontPage("AN");

        _readRepository.Verify(r => r.GetFrontPageAsync(It.IsAny<string>(), It.IsAny<FrontPageQuery>()), Times.Never);
    }

    [Theory]
    [InlineData(0, 3, null, null)]
    [InlineData(51, 3, null, null)]
    [InlineData(6, -1, null, null)]
    [InlineData(6, 21, null, null)]
    [InlineData(6, 3, -1, null)]
    [InlineData(6, 3, null, 1001)]
    public async Task InvalidLayout_IsABadRequest(int top, int perSection, int? excerpt, int? heroExcerpt)
    {
        var result = await _controller.GetFrontPage("EU", top, perSection, null, excerpt, heroExcerpt);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Health; DROP")]
    [InlineData("frontpage:EU:keys")]
    public async Task SectionMustBeASectionName(string section)
    {
        var result = await _controller.GetFrontPage("EU", section: section);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UnknownLocation_IsABadRequest()
    {
        var result = await _controller.GetFrontPage("XX");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
