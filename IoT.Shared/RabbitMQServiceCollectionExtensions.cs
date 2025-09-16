using IoT.Shared.Messaging;
using IoT.Shared.Mq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Text.Json;

namespace IoT.Shared;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration
        var rabbitOptions = configuration.GetSection("Rabbit").Get<RabbitOptions>() ?? new RabbitOptions();
        services.AddSingleton(rabbitOptions);

        // Register RabbitMQ connection
        services.AddSingleton(sp =>
        {
            var factory = new ConnectionFactory { Uri = new Uri(rabbitOptions.Uri) };
            return factory.CreateConnection();
        });

        // Register RabbitMQ channel and declare exchanges/queues/bindings
        services.AddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            var channel = connection.CreateModel();

            // Declare exchanges
            foreach (var ex in rabbitOptions.Exchanges)
            {
                channel.ExchangeDeclare(
                    exchange: ex.Name,
                    type: ex.Type,
                    durable: ex.Durable);
            }

            // Declare queues and bindings
            foreach (var q in rabbitOptions.Queues)
            {
                channel.QueueDeclare(
                    queue: q.Name,
                    durable: q.Durable,
                    exclusive: q.Exclusive,
                    autoDelete: q.AutoDelete,
                    arguments: null);

                // Bind queue to exchanges
                foreach (var b in q.Bindings)
                {
                    channel.QueueBind(
                        queue: q.Name,
                        exchange: b.Exchange,
                        routingKey: b.RoutingKey);
                }
            }

            return channel;
        });

        // JSON serializer options
        services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
        });

        // Publisher service
        services.AddSingleton<IMessagePublisher, RabbitPublisher>();

        return services;
    }
}