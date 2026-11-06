using Messaging.Events;
using Messaging.Exchanges;
using Microsoft.Extensions.Options;
using NewsletterService.Queues;
using NewsletterService.Shared.Options;
using RabbitMQ.Client;

namespace NewsletterService.Setup;

public static class Messaging
{
    extension(WebApplicationBuilder builder)
    {
        public void ConfigureRabbitMq()
        {
            builder.AddConnectionFactory();
            
            // Register the message handler
            builder.Services.AddScoped<IMessageHandler<NewSubscriberEvent>, NewSubscriberMessageHandler>();
            
            // Register the queue consumers
            builder.Services.AddHostedService<NewSubscriberConsumer>();
        }

        private void AddConnectionFactory()
        {
            builder.Services.Configure<RabbitMqOptions>(
                builder.Configuration.GetSection("RabbitMQ"));

            builder.Services.AddSingleton(sp =>
            {
                var options = sp
                    .GetRequiredService<IOptions<RabbitMqOptions>>()
                    .Value;

                return new ConnectionFactory
                {
                    HostName = options.HostName,
                    UserName = options.UserName,
                    Password = options.Password
                };
            });
        }
    }

    public static async Task ConfigureExchangesAsync(
        this WebApplication app)
    {
        var factory = app.Services
            .GetRequiredService<ConnectionFactory>();

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await SubscribersExchange.ConfigureAsync(channel);
    }
}
