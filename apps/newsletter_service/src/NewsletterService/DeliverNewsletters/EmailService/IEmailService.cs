using System.Text.Json;

namespace NewsletterService.DeliverNewsletters.EmailService;

public interface IEmailService
{
    public bool SendEmail(Email email);
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
}