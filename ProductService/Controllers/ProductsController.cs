using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProductService.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMongoCollection<Product> _products;
    private readonly IConnectionMultiplexer _redis;

    public ProductsController(IMongoClient mongo, IConnectionMultiplexer redis,
        IConfiguration config)
    {
        var db = mongo.GetDatabase(config["MongoDB:Database"]);
        _products = db.GetCollection<Product>("products");
        _redis = redis;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var cache = _redis.GetDatabase();
        var cached = await cache.StringGetAsync("products:all");
        if (cached.HasValue)
        {
            var cachedStr = cached.ToString();
            return Ok(JsonSerializer.Deserialize<List<Product>>(cachedStr));
        }

        var products = await _products.Find(_ => true).ToListAsync();
        await cache.StringSetAsync("products:all",
            JsonSerializer.Serialize(products),
            TimeSpan.FromMinutes(5));

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create(Product product)
    {
        await _products.InsertOneAsync(product);
        var cache = _redis.GetDatabase();
        await cache.KeyDeleteAsync("products:all");
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _products.DeleteOneAsync(p => p.Id == id);
        var cache = _redis.GetDatabase();
        await cache.KeyDeleteAsync("products:all");
        return NoContent();
    }
}
