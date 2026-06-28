namespace OrderService.Models;

public record CreateOrderDto(
    string ProductId,
    int Quantity,
    decimal TotalPrice,
    string CustomerEmail
);