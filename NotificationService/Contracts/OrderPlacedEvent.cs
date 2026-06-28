namespace Shared.Contracts;

public record OrderPlacedEvent(
    Guid OrderId,
    string ProductId,
    int Quantity,
    decimal TotalPrice,
    string CustomerEmail,
    DateTime PlacedAt
);