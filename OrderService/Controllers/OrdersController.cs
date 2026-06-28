using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using Shared.Contracts;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _bus;

    public OrdersController(OrderDbContext db, IPublishEndpoint bus)
    {
        _db  = db;
        _bus = bus;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetAll()
        => Ok(await _db.Orders.OrderByDescending(o => o.PlacedAt).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<Order>> PlaceOrder(CreateOrderDto dto)
    {
        var order = new Order
        {
            ProductId     = dto.ProductId,
            Quantity      = dto.Quantity,
            TotalPrice    = dto.TotalPrice,
            CustomerEmail = dto.CustomerEmail
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _bus.Publish(new OrderPlacedEvent(
            order.Id,
            order.ProductId,
            order.Quantity,
            order.TotalPrice,
            order.CustomerEmail,
            order.PlacedAt));

        return CreatedAtAction(nameof(GetAll), new { id = order.Id }, order);
    }
}