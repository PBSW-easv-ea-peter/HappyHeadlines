using System.Net;
using System.Text;
using DraftService.Profanity;
using Microsoft.Extensions.Logging;
using Moq;
using Polly.CircuitBreaker;
using Xunit;

namespace DraftService.Tests.Profanity;

// The client must fail closed: only a real answer from ProfanityService may say "no banned
// words". Every failure has to come back as Unavailable, or unchecked text gets through.
public class ProfanityClientTests
{
    private static ProfanityClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) =>
        new(new HttpClient(new StubHandler(send)) { BaseAddress = new Uri("http://profanity.test") },
            Mock.Of<ILogger<ProfanityClient>>());

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task CheckAsync_CleanText_ReturnsNoBannedWords()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "[]")));

        var result = await client.CheckAsync("lovely");

        Assert.False(result.Unavailable);
        Assert.False(result.IsProfane);
    }

    [Fact]
    public async Task CheckAsync_ProfaneText_ReturnsBannedWords()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "[\"idiot\"]")));

        var result = await client.CheckAsync("you idiot");

        Assert.False(result.Unavailable);
        Assert.Equal(["idiot"], result.BannedWords);
    }

    public static TheoryData<string, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> Failures => new()
    {
        { "connection refused", (_, _) => throw new HttpRequestException("Connection refused") },
        { "timeout", (_, _) => throw new TaskCanceledException("HttpClient.Timeout elapsed") },
        { "circuit open", (_, _) => throw new BrokenCircuitException() },
        { "500 response", (_, _) => Task.FromResult(Json(HttpStatusCode.InternalServerError, "oops")) },
        { "null body", (_, _) => Task.FromResult(Json(HttpStatusCode.OK, "null")) },
        { "invalid body", (_, _) => Task.FromResult(Json(HttpStatusCode.OK, "<html>")) },
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task CheckAsync_NoAnswerFromProfanityService_IsUnavailable(
        string failure, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        var client = CreateClient(send);

        var result = await client.CheckAsync("you idiot");

        Assert.True(result.Unavailable, $"{failure} must not count as a clean check");
    }

    [Fact]
    public async Task CheckAsync_CallerCancels_Throws()
    {
        // The request was aborted, so there is nothing to classify - don't swallow it.
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var client = CreateClient((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CheckAsync("text", cts.Token));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
