using Moq;
using ProfanityService.Checking;
using ProfanityService.Repositories;
using Xunit;

namespace ProfanityService.Tests.Checking;

public class ProfanityCheckerTests
{
    private readonly Mock<IProfanityRepository> _repository = new();
    private readonly ProfanityChecker _checker;

    public ProfanityCheckerTests()
    {
        _checker = new ProfanityChecker(_repository.Object);
    }

    [Fact]
    public async Task CheckAsync_SplitsTextIntoDistinctWords_CaseInsensitive()
    {
        IEnumerable<string>? capturedWords = null;
        _repository
            .Setup(r => r.FindBannedWordsAsync(It.IsAny<IEnumerable<string>>()))
            .Callback<IEnumerable<string>>(words => capturedWords = words)
            .ReturnsAsync(Array.Empty<string>());

        await _checker.CheckAsync("Dum dum idiot");

        Assert.NotNull(capturedWords);
        Assert.Equal(new[] { "Dum", "idiot" }, capturedWords);
    }

    [Fact]
    public async Task CheckAsync_ReturnsRepositoryResultUnchanged()
    {
        _repository
            .Setup(r => r.FindBannedWordsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { "idiot" });

        var result = await _checker.CheckAsync("You are an idiot");

        Assert.Equal(new[] { "idiot" }, result);
    }

    [Fact]
    public async Task CheckAsync_NoMatches_ReturnsEmptyList()
    {
        _repository
            .Setup(r => r.FindBannedWordsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(Array.Empty<string>());

        var result = await _checker.CheckAsync("A perfectly clean sentence");

        Assert.Empty(result);
    }
}
