namespace NewsletterService.Services.SubscriberService;

public class Subscriber
{
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required Interest[] Interests { get; set; }
}

public class Interest
{
    public required string Section  { get; set; }
    public required int Region { get; set; }
}