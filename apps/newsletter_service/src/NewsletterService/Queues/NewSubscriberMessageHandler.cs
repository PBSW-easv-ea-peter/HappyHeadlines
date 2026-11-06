using Messaging.Events;
using NewsletterService.DeliverNewsletters.EmailService;

namespace NewsletterService.Queues;

/// <summary>
/// Handles new subscriber messages by sending welcome emails.
/// This class contains the business logic for processing new subscriber events,
/// </summary>
public class NewSubscriberMessageHandler : IMessageHandler<NewSubscriberEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<NewSubscriberMessageHandler> _logger;

    public NewSubscriberMessageHandler(
        IEmailService emailService,
        ILogger<NewSubscriberMessageHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public Task HandleAsync(NewSubscriberEvent newSubscriber, CancellationToken ct = default)
    {
        _logger.LogInformation("Subscriber email: {Email}", newSubscriber.Email);

        Email welcomeEmail = new()
        {
            EmailAddress = newSubscriber.Email,
            NameOfReceiver = newSubscriber.Username,
            Subject = "Welcome to Happy Headlines",
            Content = $"We're happy to have you onboard our newsletter {newSubscriber.Username}!"
        };

        bool sent = _emailService.SendEmail(welcomeEmail);

        if (!sent)
            throw new ApplicationException("Unable to send email");
        
        return Task.CompletedTask;
    }
}
