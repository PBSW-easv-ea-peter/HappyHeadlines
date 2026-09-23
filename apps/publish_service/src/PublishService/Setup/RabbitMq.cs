using RabbitMQ.Client;
using Messaging.Exchanges;
using Microsoft.Extensions.Options;
using PublishService.Models.Options;

namespace PublishService.Setup;

public static class RabbitMq
{
    public static WebApplicationBuilder ConfigureRabbitMq(
        this WebApplicationBuilder builder)
    {
        builder.AddConnectionFactory();

        return builder;
    }

    private static WebApplicationBuilder AddConnectionFactory(
        this WebApplicationBuilder builder)
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

        return builder;
    }

    public static async Task ConfigureExchangesAsync(
        this WebApplication app)
    {
        var factory = app.Services
            .GetRequiredService<ConnectionFactory>();

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await PublishedArticlesExchange.ConfigureAsync(channel);
    }
}
