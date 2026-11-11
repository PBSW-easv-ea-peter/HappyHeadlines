using Messaging.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NewsletterService.NewsletterDistributors.EmailService;
using NewsletterService.Queues.NewSubscriber;

namespace NewsletterService.Tests.Queue;

public class NewSubscriberMessageHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IEmailFactory> _emailFactory = new();
    private readonly NewSubscriberMessageHandler _handler;

    public NewSubscriberMessageHandlerTests()
    {
        _handler = new NewSubscriberMessageHandler(
            _emailService.Object, 
            _emailFactory.Object, 
            NullLogger<NewSubscriberMessageHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SendsWelcomeEmail()
    {
        // Arrange
        _emailFactory
            .Setup(f => f.CreateWelcomeEmail(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns((string email, string nameOfReceiver) => new Email
            {
                EmailAddress = email,
                NameOfReceiver = nameOfReceiver,
                Subject = "Welcome to Happy Headlines",
                Content = $"We're happy to have you onboard our newsletter {nameOfReceiver}!"
            });
        
        var testEvent = new NewSubscriberEvent
        {
            Username = "TestUser",
            Email = "test@example.com"
        };

        _emailService.Setup(e => e.SendEmail(It.IsAny<Email>())).Returns(true);

        // Act
        await _handler.HandleAsync(testEvent, CancellationToken.None);

        // Assert
        _emailService.Verify(e => e.SendEmail(It.Is<Email>(email =>
            email.EmailAddress == "test@example.com" &&
            email.NameOfReceiver == "TestUser" &&
            email.Subject == "Welcome to Happy Headlines" &&
            email.Content.Contains("TestUser"))), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenEmailSendFails()
    {
        // Arrange
        var testEvent = new NewSubscriberEvent
        {
            Username = "TestUser",
            Email = "test@example.com"
        };

        _emailService.Setup(e => e.SendEmail(It.IsAny<Email>())).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => 
            _handler.HandleAsync(testEvent, CancellationToken.None));
    }
}
