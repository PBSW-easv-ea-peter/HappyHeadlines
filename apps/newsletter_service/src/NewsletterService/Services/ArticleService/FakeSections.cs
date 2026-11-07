namespace NewsletterService.Services.ArticleService;

public static class FakeSections
{
    public static readonly Section[] Data =
    [
        new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Politics" },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Culture" },
        new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Sports" }
    ];
}