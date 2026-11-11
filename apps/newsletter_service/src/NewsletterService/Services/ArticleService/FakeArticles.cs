using NewsletterService.Shared;

namespace NewsletterService.Services.ArticleService;

public static class FakeArticles
{
    public static readonly List<Article> Data =
    [

        // ----- Global -----
        new Article
        {
            Title = "UN Climate Summit Ends With Historic Carbon Accord",
            Byline = "By Amara Okafor, Diplomatic Correspondent",
            BreadText =
                "Delegates from 194 nations agreed on a binding framework to cut carbon emissions by 45% before 2035, ending two weeks of fraught negotiations in Geneva. The accord establishes a shared verification body and a fund to help developing nations transition their energy grids. Critics called the targets ambitious but warned that enforcement mechanisms remain vague. \"This is the first time the world has moved from pledges to accountability,\" said the summit chair at the closing press conference.",
            PublishDate = DateTimeOffset.Parse("2026-11-03T08:15:00Z"),
            Location = (int)Regions.Global,
            SectionName = "Politics"
        },

        new Article
        {
            Title = "World Music Festival Draws Record Two Million Viewers Online",
            Byline = "By Lucien Marchand, Arts Critic",
            BreadText =
                "The eighth edition of the World Music Festival brought together 120 artists from 40 countries in a single weekend, streamed free across four platforms. Organizers reported more than two million concurrent viewers during Saturday's closing set, nearly triple last year's peak. Performances ranged from Mongolian throat singing to Afro-Cuban jazz, with several collaborations premiered for the first time. Analysts credited the surge to improved subtitling and a new interactive setlist feature.",
            PublishDate = DateTimeOffset.Parse("2026-11-04T14:30:00Z"),
            Location = (int)Regions.Global,
            SectionName = "Culture"
        },

        new Article
        {
            Title = "International Chess League Crowns Its First Global Champion",
            Byline = "By Priya Raghavan, Sports Desk",
            BreadText =
                "Grandmaster Elina Kovacs of Romania claimed the International Chess League's inaugural global title after a tense tiebreak series in Reykjavik. The decisive game lasted 71 moves and was watched live by an estimated 12 million viewers. Kovacs, 24, secured victory with a rook endgame that commentators described as textbook-perfect. The league, founded in 2024, said prize money for next season will double on the back of growing sponsorship interest.",
            PublishDate = DateTimeOffset.Parse("2026-11-05T19:45:00Z"),
            Location = (int)Regions.Global,
            SectionName = "Sports"
        },

        // ----- Europe -----

        new Article
        {
            Title = "EU Parliament Approves Sweeping Energy Market Reform",
            Byline = "By Ingrid Bauer, Brussels Bureau",
            BreadText =
                "The European Parliament voted 412 to 179 in favour of the most significant overhaul of the bloc's electricity market in two decades. The reform caps wholesale prices during acute supply crises and speeds up permitting for cross-border grid interconnections. Member states now have 18 months to transpose the rules into national law. Energy analysts expect the changes to reduce household bills modestly, though some warned that the cap mechanisms could discourage investment in new capacity.",
            PublishDate = DateTimeOffset.Parse("2026-11-02T10:00:00+01:00"),
            Location = (int)Regions.Europe,
            SectionName = "Politics"
        },

        new Article
        {
            Title = "Restored Rembrandt Revealed at Rijksmuseum After Five-Year Effort",
            Byline = "By Sofie de Vries, Culture Editor",
            BreadText =
                "The Rijksmuseum unveiled its newly restored Rembrandt on Wednesday, the culmination of five years of microscopic cleaning and pigment analysis. Restorers removed layers of yellowed varnish applied in the nineteenth century, recovering the painting's original depth of colour and subtle chiaroscuro. The museum's head of conservation called the result a \"reunion with the master's own hand.\" The painting goes on public display from Friday, with timed tickets already sold out for the first month.",
            PublishDate = DateTimeOffset.Parse("2026-11-03T11:20:00+01:00"),
            Location = (int)Regions.Europe,
            SectionName = "Culture"
        },

        new Article
        {
            Title = "Champions League Underdogs Stun Defending Champions in Rotterdam",
            Byline = "By Marco Ferretti, Football Correspondent",
            BreadText =
                "A stoppage-time header from captain Daan Vermeer gave the hosts a 2-1 victory that few predicted. The underdogs defended deep for an hour before twice catching the champions on the counter-attack. \"Nobody gave us a chance, and that was our fuel,\" said Vermeer after the match. The result leaves the defending champions third in their group with two matches remaining, while the Rotterdam side tops the table on goal difference.",
            PublishDate = DateTimeOffset.Parse("2026-11-04T22:05:00+01:00"),
            Location = (int)Regions.Europe,
            SectionName = "Sports"
        },

        // ----- North America -----

        new Article
        {
            Title = "Congress Reaches Late-Night Deal to Avert Shutdown",
            Byline = "By Daniel Reyes, Washington Desk",
            BreadText =
                "Lawmakers struck a compromise hours before federal funding was set to lapse, passing a short-term spending bill that keeps agencies open through January. The deal includes disaster relief funds but defers contentious budget fights to the next session. Both sides claimed victories, though analysts noted the can has merely been kicked down the road. Passage came after an intense 48 hours of negotiation between committee chairs and the executive office.",
            PublishDate = DateTimeOffset.Parse("2026-11-05T03:40:00-05:00"),
            Location = (int)Regions.NorthAmerica,
            SectionName = "Politics"
        },

        new Article
        {
            Title = "Indigenous Art Exhibition Opens to Critical Acclaim in Toronto",
            Byline = "By Emily Carver, Arts Writer",
            BreadText =
                "Featuring 60 works from more than a dozen First Nations, the exhibition spans sculpture, beadwork, painting, and new media. Critics have called it the most significant survey of contemporary Indigenous art in a decade. Curator Sarah Whitecrow said the show deliberately pairs traditional techniques with digital installations to challenge museum conventions. The gallery has extended opening hours through the winter after advance bookings exceeded expectations.",
            PublishDate = DateTimeOffset.Parse("2026-11-05T16:10:00-05:00"),
            Location = (int)Regions.NorthAmerica,
            SectionName = "Culture"
        },

        new Article
        {
            Title = "Rookie Quarterback Throws Five Touchdowns in Breakout Performance",
            Byline = "By Jake Thompson, Sports Reporter",
            BreadText =
                "The 22-year-old completed 28 of 33 passes for 412 yards in a 38-17 rout that announced his arrival as a franchise cornerstone. Coaches praised his pre-snap reads, and teammates described a calmness in the huddle that belied his inexperience. \"He played like a ten-year veteran tonight,\" said the head coach at the post-game podium. The win moves the team to the top of its division with six games remaining in the regular season.",
            PublishDate = DateTimeOffset.Parse("2026-11-06T23:50:00-05:00"),
            Location = (int)Regions.NorthAmerica,
            SectionName = "Sports"
        }
    ];
}
