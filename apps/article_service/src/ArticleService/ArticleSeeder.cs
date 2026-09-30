using ArticleService.Sharding;
using Dapper;
using Npgsql;
using System.Net.Sockets;

namespace ArticleService.Seeding;

public class ArticleSeeder : IHostedService
{
    private readonly IArticleShardResolver _shardResolver;
    private readonly ILogger<ArticleSeeder> _logger;

    public ArticleSeeder(
        IArticleShardResolver shardResolver,
        ILogger<ArticleSeeder> logger)
    {
        _shardResolver = shardResolver;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding articles...");

        await Task.Delay(10000);

        foreach (var location in Articles
                     .Select(article => article.Location)
                     .Distinct())
        {
            try
            {
                await SeedLocationAsync(location, cancellationToken);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogWarning(
                    "Could not connect to the {Location} article database. " +
                    "Skipping seeding for this database. Error: {Message}",
                    location,
                    ex.Message);
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(
                        "Could not connect to the {Location} article database. " +
                        "Skipping seeding for this database. Error: {Message}",
                        location,
                        ex.Message);
            }
        }

        _logger.LogInformation("Article seeding completed.");
    }

    private async Task SeedLocationAsync(
        string location,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(
            _shardResolver.GetConnectionString(location));

        await connection.OpenAsync(cancellationToken);

        foreach (var article in Articles.Where(x => x.Location == location))
        {
            const string sql = """
                insert into articles
                    (byline, title, breadtext, publish_date, location, section_id)
                select
                    @Byline,
                    @Title,
                    @Breadtext,
                    @PublishDate,
                    @Location,
                    @SectionId
                where not exists (
                    select 1
                    from articles
                    where title = @Title
                      and location = @Location
                )
                """;

            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        article.Byline,
                        article.Title,
                        article.Breadtext,
                        article.PublishDate,
                        article.Location,
                        article.SectionId
                    },
                    cancellationToken: cancellationToken));

            if (rowsAffected > 0)
            {
                _logger.LogInformation(
                    "Seeded article '{Title}' into {Location}.",
                    article.Title,
                    article.Location);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private sealed record SeedArticle(
        string Byline,
        string Title,
        string Breadtext,
        DateOnly PublishDate,
        string Location,
        int SectionId);

    private static readonly SeedArticle[] Articles =
    [
        new(
            "Emma Johnson",
            "Global Climate Summit 2026: Key Takeaways",
            "World leaders gathered to discuss urgent climate actions. Agreements were made to reduce carbon emissions by 50% by 2035.",
            new DateOnly(2026, 9, 10),
            "GO",
            3),

        new(
            "Liam Chen",
            "AI in 2026: The Next Frontier",
            "Artificial intelligence continues to transform industries. Experts predict AI will replace 30% of repetitive jobs within a decade.",
            new DateOnly(2026, 9, 11),
            "GO",
            2),

        new(
            "Sophia Garcia",
            "Pandemic Preparedness: Lessons Learned",
            "Countries are investing in healthcare infrastructure to prevent future pandemics. Vaccine development has accelerated.",
            new DateOnly(2026, 9, 12),
            "GO",
            4),

        new(
            "Noah Wilson",
            "EU Energy Crisis: Solutions and Challenges",
            "Europe faces an energy crisis as it transitions to renewable sources. Governments are debating nuclear energy as a temporary solution.",
            new DateOnly(2026, 9, 1),
            "EU",
            3),

        new(
            "Olivia Brown",
            "The Rise of Remote Work in Europe",
            "Remote work is becoming the norm in Europe. Companies are adopting hybrid models to attract talent.",
            new DateOnly(2026, 9, 2),
            "EU",
            2),

        new(
            "James Lee",
            "Brexit Aftermath: Economic Impact on EU",
            "The UK's exit from the EU continues to shape trade and immigration policies. Businesses adapt to new regulations.",
            new DateOnly(2026, 9, 3),
            "EU",
            1),

        new(
            "Ava Martinez",
            "US Election 2026: Early Predictions",
            "Analysts predict a highly polarized election. Key issues include healthcare, immigration, and climate change.",
            new DateOnly(2026, 9, 4),
            "NA",
            1),

        new(
            "Ethan Davis",
            "Canada's Wildfire Crisis: A Climate Warning",
            "Wildfires in Canada have destroyed millions of acres. Experts link the fires to rising global temperatures.",
            new DateOnly(2026, 9, 5),
            "NA",
            3),

        new(
            "Emma Johnson",
            "Tech Giants and Antitrust Laws in the US",
            "The US government is tightening regulations on tech monopolies. Lawsuits against major corporations are on the rise.",
            new DateOnly(2026, 9, 6),
            "NA",
            2),

        new(
            "Liam Chen",
            "Amazon Rainforest: Deforestation at a Record Low",
            "Brazil reports a 40% reduction in deforestation due to stricter environmental laws and international pressure.",
            new DateOnly(2026, 9, 7),
            "SA",
            3),

        new(
            "Sophia Garcia",
            "Argentina's Economic Recovery: A New Era",
            "Argentina's economy shows signs of recovery after years of inflation. New policies aim to stabilize the currency.",
            new DateOnly(2026, 9, 8),
            "SA",
            1),

        new(
            "Noah Wilson",
            "The Cultural Renaissance of Peru",
            "Peru is experiencing a cultural revival, with indigenous traditions gaining global recognition.",
            new DateOnly(2026, 9, 9),
            "SA",
            5),

        new(
            "Olivia Brown",
            "Australia's Great Barrier Reef: A Conservation Success",
            "Efforts to restore the Great Barrier Reef are showing positive results. Coral coverage has increased by 20%.",
            new DateOnly(2026, 9, 10),
            "AU",
            3),

        new(
            "James Lee",
            "New Zealand's Tourism Boom Post-Pandemic",
            "New Zealand is seeing a surge in tourism as travelers return. The government promotes sustainable tourism.",
            new DateOnly(2026, 9, 11),
            "AU",
            5),

        new(
            "Ava Martinez",
            "The Indigenous Rights Movement in Australia",
            "Indigenous communities in Australia are gaining more recognition and rights. Land acknowledgments are now common.",
            new DateOnly(2026, 9, 12),
            "AU",
            1),

        new(
            "Ethan Davis",
            "China's Tech Dominance: A Global Concern",
            "China's advancements in AI and 5G are raising concerns about global tech dominance. Countries debate bans on Chinese tech.",
            new DateOnly(2026, 9, 1),
            "AS",
            2),

        new(
            "Emma Johnson",
            "India's Space Mission: A New Milestone",
            "India successfully launched its first manned space mission, marking a significant achievement in space exploration.",
            new DateOnly(2026, 9, 2),
            "AS",
            2),

        new(
            "Liam Chen",
            "Japan's Aging Population: Solutions and Innovations",
            "Japan is tackling its aging population with robotics and AI. The government encourages immigration to boost the workforce.",
            new DateOnly(2026, 9, 3),
            "AS",
            4),

        new(
            "Sophia Garcia",
            "Africa's Green Energy Revolution",
            "African countries are leading in renewable energy adoption. Solar and wind projects are expanding rapidly.",
            new DateOnly(2026, 9, 4),
            "AF",
            3),

        new(
            "Noah Wilson",
            "South Africa's Economic Reforms: A Path to Growth",
            "South Africa is implementing economic reforms to attract foreign investment and reduce unemployment.",
            new DateOnly(2026, 9, 5),
            "AF",
            1),

        new(
            "Olivia Brown",
            "The Rise of Afrofuturism in Pop Culture",
            "Afrofuturism is gaining popularity in music, film, and literature. Artists are reimagining Africa's future.",
            new DateOnly(2026, 9, 6),
            "AF",
            5),

        new(
            "James Lee",
            "Antarctica's Melting Ice: A Climate Emergency",
            "Scientists report record ice melt in Antarctica. Rising sea levels threaten coastal cities worldwide.",
            new DateOnly(2026, 9, 7),
            "AN",
            3),

        new(
            "Ava Martinez",
            "Research in Antarctica: Discovering New Species",
            "New species of marine life have been discovered in Antarctica's icy waters. Researchers study their adaptations.",
            new DateOnly(2026, 9, 8),
            "AN",
            2),

        new(
            "Ethan Davis",
            "The Geopolitics of Antarctica: Who Owns the Ice?",
            "Countries are staking claims in Antarctica for its resources. International treaties aim to prevent conflicts.",
            new DateOnly(2026, 9, 9),
            "AN",
            1)
    ];
}
