using MassTransit;
using Shared.Contracts;

namespace NotificationService.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
        => _logger = logger;

    public Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation(
            "📧 Sending email to {Email} — Order {OrderId} placed for product {ProductId}, qty {Qty}, total £{Total}",
            e.CustomerEmail, e.OrderId, e.ProductId, e.Quantity, e.TotalPrice);

        return Task.CompletedTask;
    }
}