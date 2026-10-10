using System.Text.Json;
using Confluent.Kafka;
using Medzo.PurchasingSupplier.Application.PurchaseOrders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Medzo.PurchasingSupplier.Infrastructure.Messaging;

public sealed class KafkaPurchaseOrderEventPublisher(IConfiguration configuration, ILogger<KafkaPurchaseOrderEventPublisher> logger) : IPurchaseOrderEventPublisher
{
    public async Task PublishReceivedAsync(PurchaseOrderStockReceivedEvent message, CancellationToken cancellationToken)
    {
        if (!bool.TryParse(configuration["Kafka:Enabled"], out var enabled) || !enabled)
        {
            logger.LogWarning("Kafka is disabled; stock receipt event for {PurchaseOrderNumber} was not published.", message.PurchaseOrderNumber);
            return;
        }

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            EnableIdempotence = !bool.TryParse(configuration["Kafka:EnableIdempotence"], out var enableIdempotence) || enableIdempotence,
            Acks = Acks.All
        };
        ConfigureSecurity(config, configuration);
        using var producer = new ProducerBuilder<string, string>(config).Build();
        var payload = JsonSerializer.Serialize(new { eventId = message.EventId, eventType = "purchasing.purchase-recorded.v1", eventVersion = 1, occurredAtUtc = message.ReceivedAtUtc, producer = "medzo-purchasing-supplier-service", data = new { sourceId = message.PurchaseOrderNumber, lines = message.Lines } });
        await producer.ProduceAsync("purchasing.purchase-recorded.v1", new Message<string, string> { Key = message.PurchaseOrderNumber, Value = payload }, cancellationToken);
    }

    private static void ConfigureSecurity(ClientConfig config, IConfiguration configuration)
    {
        if (Enum.TryParse<SecurityProtocol>(configuration["Kafka:SecurityProtocol"], true, out var securityProtocol))
            config.SecurityProtocol = securityProtocol;

        if (Enum.TryParse<SaslMechanism>(configuration["Kafka:SaslMechanism"], true, out var saslMechanism))
            config.SaslMechanism = saslMechanism;

        var username = configuration["Kafka:SaslUsername"];
        var password = configuration["Kafka:SaslPassword"];
        if (!string.IsNullOrWhiteSpace(username)) config.SaslUsername = username;
        if (!string.IsNullOrWhiteSpace(password)) config.SaslPassword = password;
    }
}
