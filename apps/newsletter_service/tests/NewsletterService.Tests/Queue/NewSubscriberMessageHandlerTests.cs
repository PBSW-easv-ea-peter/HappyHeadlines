using Messaging.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NewsletterService.DeliverNewsletters.EmailService;
using NewsletterService.Queues;

namespace NewsletterService.Tests.Queue;

public class NewSubscriberMessageHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly NewSubscriberMessageHandler _handler;

    public NewSubscriberMessageHandlerTests()
    {
        _handler = new NewSubscriberMessageHandler(_emailService.Object, NullLogger<NewSubscriberMessageHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_SendsWelcomeEmail()
    {
        // Arrange
        var testEvent = new NewSubscriberEvent
        {
            Username = "TestUser",
            Email = "test@example.com",
            Intervals = [1, 2, 3],
            SectionIds = [1, 2],
            RegionIds = [1]
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
            Email = "test@example.com",
            Intervals = [1],
            SectionIds = [1],
            RegionIds = [1]
        };

        _emailService.Setup(e => e.SendEmail(It.IsAny<Email>())).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => 
            _handler.HandleAsync(testEvent, CancellationToken.None));
    }
}
