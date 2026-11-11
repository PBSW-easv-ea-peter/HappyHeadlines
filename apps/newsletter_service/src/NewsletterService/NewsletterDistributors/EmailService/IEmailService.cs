using System.Text.Json;

namespace NewsletterService.NewsletterDistributors.EmailService;

public interface IEmailService
{
    public bool SendEmail(Email email);
    
    public bool SendEmails(IEnumerable<Email> emails);
}

public class FakeEmailService : IEmailService
{
    private readonly IEmailSink _emailSink;
    private readonly ILogger<FakeEmailService> _logger;
    
    public FakeEmailService(
        IEmailSink sink,
        ILogger<FakeEmailService> logger)
    {
        _emailSink = sink;
        _logger = logger;
    }

    public bool SendEmail(Email email)
    {
        _emailSink.AddEmail(email);
        
        var json = JsonSerializer.Serialize(email, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        _logger.LogInformation("FakeEmailService captured email:\n{Email}", json);

        
        return true;
    }
    
    public bool SendEmails(IEnumerable<Email> emails)
    {
        foreach (Email email in emails)
        {
            _emailSink.AddEmail(email);
        }

        _logger.LogInformation("FakeEmailService captured emails count: {Count}", emails.Count());
        
        return true;
    }
}