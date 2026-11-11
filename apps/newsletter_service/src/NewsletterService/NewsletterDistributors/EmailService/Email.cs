namespace NewsletterService.NewsletterDistributors.EmailService;

public class Email
{
    public required string EmailAddress { get; set; }
    public string? NameOfReceiver { get; set; }
    public required string Subject { get; set; }
    public required string Content { get; set; }
}