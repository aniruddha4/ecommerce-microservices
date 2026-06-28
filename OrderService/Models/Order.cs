namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}