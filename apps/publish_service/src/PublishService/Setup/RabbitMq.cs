using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using PublishService.Shared;
using PublishService.Shared.Options;

namespace PublishService.Setup;

public static class RabbitMq
{
    extension(WebApplicationBuilder builder)
    {
        public void ConfigureRabbitMq()
        {
            builder.AddConnectionFactory();
            builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
        }
    }

    private static void AddConnectionFactory(this WebApplicationBuilder builder)
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
