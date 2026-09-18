using Microsoft.AspNetCore.Mvc;
using Moq;
using ProfanityService.Checking;
using ProfanityService.Controllers;
using ProfanityService.Models;
using Xunit;

namespace ProfanityService.Tests.Controllers;

public class ProfanityControllerTests
{
    private readonly Mock<IProfanityChecker> _checker = new();
    private readonly ProfanityController _controller;

    public ProfanityControllerTests()
    {
        _controller = new ProfanityController(_checker.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Check_EmptyText_ReturnsBadRequest(string text)
    {
        var result = await _controller.Check(new ProfanityCheckRequest { Text = text });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Check_CleanText_ReturnsEmptyList()
    {
        _checker.Setup(c => c.CheckAsync("A clean sentence")).ReturnsAsync(Array.Empty<string>());

        var result = await _controller.Check(new ProfanityCheckRequest { Text = "A clean sentence" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty((IReadOnlyList<string>)ok.Value!);
    }

    [Fact]
    public async Task Check_ProfaneText_ReturnsBannedWords()
    {
        _checker.Setup(c => c.CheckAsync("You are an idiot")).ReturnsAsync(new[] { "idiot" });

        var result = await _controller.Check(new ProfanityCheckRequest { Text = "You are an idiot" });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(new[] { "idiot" }, (IReadOnlyList<string>)ok.Value!);
    }
}
