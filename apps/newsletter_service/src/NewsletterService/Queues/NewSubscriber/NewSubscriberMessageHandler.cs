using Messaging.Events;
using NewsletterService.NewsletterDistributors.EmailService;

namespace NewsletterService.Queues.NewSubscriber;

/// <summary>
/// Handles new subscriber messages by sending welcome emails.
/// This class contains the business logic for processing new subscriber events,
/// </summary>
public class NewSubscriberMessageHandler : IMessageHandler<NewSubscriberEvent>
{
    private readonly IEmailService _emailService;
    private readonly IEmailFactory _emailFactory;
    private readonly ILogger<NewSubscriberMessageHandler> _logger;

    public NewSubscriberMessageHandler(
        IEmailService emailService,
        IEmailFactory emailFactory,
        ILogger<NewSubscriberMessageHandler> logger)
    {
        _emailService = emailService;
        _emailFactory = emailFactory;
        _logger = logger;
    }

    public Task HandleAsync(NewSubscriberEvent newSubscriber, CancellationToken ct = default)
    {
        _logger.LogInformation("Subscriber email: {Email}", newSubscriber.Email);

        Email welcomeEmail = _emailFactory.CreateWelcomeEmail(newSubscriber.Email, newSubscriber.Username);

        bool sent = _emailService.SendEmail(welcomeEmail);

        if (!sent)
            throw new ApplicationException("Unable to send email");
        
        return Task.CompletedTask;
    }
}
