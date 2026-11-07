namespace NewsletterService.NewsletterDistributors.EmailService;

public interface IEmailSink
{
    public void AddEmail(Email email);
    
    // Used for testing
    public int SentEmailsCount { get; }
}

public class EmailSink : IEmailSink
{
    private readonly List<Email> _sentEmails = [];

    public void AddEmail(Email email)
    {
        _sentEmails.Add(email);
    }
    
    public int SentEmailsCount => _sentEmails.Count;
}