using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly OrdersDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;

        public ProductController(OrdersDbContext dbContext, IPublishEndpoint publishEndpoint)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            // Serilog Logging
        }

        // Whole Product Controller is for Admin only

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // check if logged in
            var allProducts = await _dbContext.Products.ToListAsync();

            if (allProducts is null || allProducts.Count == 0)
            {
                return NotFound(new { message = "There are no products in the database." });
            }
            return Ok(allProducts.Select(product => new ProductDto(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock
            )));
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetProductById(Guid productId)
        {
            // check if logged in
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(o => o.ProductId == productId);
            if (product is null)
            {
                return NotFound(new { message = $"There is no product with ID {productId} in the database." });
            }
            return Ok(new ProductDto(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock
            ));
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            // check if logged in

            // Validate input
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                return BadRequest(new { message = "Product name is required." });
            }

            if (dto.Price <= 0)
            {
                return BadRequest(new { message = "Price must be greater than 0." });
            }

            if (dto.Stock < 0)
            {
                return BadRequest(new { message = "Stock cannot be negative." });
            }

            var existingProduct = await _dbContext.Products
                .FirstOrDefaultAsync(u => u.ProductName == dto.ProductName);

            if (existingProduct != null)
            {
                return BadRequest(new { message = "Product already exists." });
            }
            var product = new Product
            {
                ProductId = Guid.NewGuid(),
                ProductName = dto.ProductName,
                Price = dto.Price,
                Stock = dto.Stock,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            // publish event CreateProductEvent
            await _publishEndpoint.Publish(new ProductCreatedEvent(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock,
                product.CreatedAt
            ));

            return CreatedAtAction(nameof(GetProductById), new { productId = product.ProductId },
            new ProductDto(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock
            ));

            
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditProduct(Guid id, EditProductDto dto)
        {
            var product = await _dbContext.Products.FindAsync(id);

            if (product is null)
            {
                return NotFound(new { message = $"There is no product with ID {id} in the database." });
            }

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(dto.ProductName) && product.ProductName != dto.ProductName)
            {
                // Check if new name already exists
                var existingProduct = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.ProductName == dto.ProductName && p.ProductId != id);

                if (existingProduct != null)
                {
                    return BadRequest(new { message = "Product name already exists." });
                }

                product.ProductName = dto.ProductName;
                hasChanges = true;
            }

            if (dto.Price.HasValue && dto.Price.Value > 0 && product.Price != dto.Price.Value)
            {
                product.Price = dto.Price.Value;
                hasChanges = true;
            }
            else if (dto.Price.HasValue && dto.Price.Value <= 0)
            {
                return BadRequest(new { message = "Price must be greater than 0." });
            }

            if (dto.Stock.HasValue && dto.Stock.Value >= 0 && product.Stock != dto.Stock.Value)
            {
                product.Stock = dto.Stock.Value;
                hasChanges = true;
            }
            else if (dto.Stock.HasValue && dto.Stock.Value < 0)
            {
                return BadRequest(new { message = "Stock cannot be negative." });
            }

            if (hasChanges)
            {
                product.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            // publish event UpdateProductEvent
            await _publishEndpoint.Publish(new ProductUpdatedEvent(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock,
                product.UpdatedAt
            ));

            return Ok(new ProductDto(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock
            ));

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            // check if logged in
            var product = await _dbContext.Products.FindAsync(id);

            if (product is null)
            {
                return NotFound(new { message = $"There is no product with ID {id} in the database." });
            }

            // Check if product is used in any orders (optional business rule)
            var hasOrders = await _dbContext.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                return BadRequest(new { message = "Cannot delete product that has been ordered." });
            }

            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();

            // publish event DeleteProductEvent
            await _publishEndpoint.Publish(new ProductDeletedEvent(
                product.ProductId,
                product.ProductName,
                DateTime.UtcNow
            ));
            
            return Ok(new { message = $"Product with ID {id} was deleted successfully." });

            // else Return Unathorized({message: "You are not authorized for this action."})
        }
    }
}
