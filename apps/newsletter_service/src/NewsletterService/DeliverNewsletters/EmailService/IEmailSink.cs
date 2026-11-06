namespace NewsletterService.DeliverNewsletters.EmailService;

public interface IEmailSink
{
    public void AddEmail(Email email);
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