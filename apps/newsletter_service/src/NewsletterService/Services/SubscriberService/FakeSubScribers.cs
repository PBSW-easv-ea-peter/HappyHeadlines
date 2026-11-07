using NewsletterService.Shared;

namespace NewsletterService.Services.SubscriberService;

public static class FakeSubScribers
{
    public static readonly Subscriber[] Data =
    [
        new Subscriber()
        {
            Email = "alice@example.com",
            Username = "alice",
            Interests =
            [
                new Interest() { Section = "Politics", Region = (int)Regions.Global },
                new Interest() { Section = "Sports", Region = (int)Regions.Europe }
            ]
        },
        new Subscriber()
        {
            Email = "bob@example.com",
            Username = "bob",
            Interests =
            [
                new Interest() { Section = "Culture", Region = (int)Regions.Global },
                new Interest() { Section = "Politics", Region = (int)Regions.Africa },
                new Interest() { Section = "Sports", Region = (int)Regions.Europe }
            ]
        },
        new Subscriber()
        {
            Email = "carol@example.com",
            Username = "carol",
            Interests =
            [
                new Interest() { Section = "Politics", Region = (int)Regions.Europe }
            ]
        },
        new Subscriber()
        {
            Email = "dave@example.com",
            Username = "dave",
            Interests =
            [
                new Interest() { Section = "Sports", Region = (int)Regions.Asia },
                new Interest() { Section = "Politics", Region = (int)Regions.NorthAmerica },
                new Interest() { Section = "Culture", Region = (int)Regions.Europe }
            ]
        },
        new Subscriber()
        {
            Email = "erin@example.com",
            Username = "erin",
            Interests =
            [
                new Interest() { Section = "Culture", Region = (int)Regions.SouthAmerica },
                new Interest() { Section = "Sports", Region = (int)Regions.Global }
            ]
        }
    ];
}
