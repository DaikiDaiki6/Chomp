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
        private readonly ILogger<ProductController> _logger;

        public ProductController(OrdersDbContext dbContext, IPublishEndpoint publishEndpoint, ILogger<ProductController> logger)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
            // Serilog Logging
        }

        // Whole Product Controller is for Admin only

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // check if logged in
            _logger.LogInformation("Getting all products - Admin Request");
            var allProducts = await _dbContext.Products.ToListAsync();

            if (allProducts is null || allProducts.Count == 0)
            {
                _logger.LogWarning("No products found in the database.");
                return NotFound(new { message = "There are no products in the database." });
            }
            
            _logger.LogInformation("Successfully retrieved {ProductCount} products.", allProducts.Count);
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
            _logger.LogInformation("Getting product - Admin Request for product {ProductId}", productId);
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(o => o.ProductId == productId);
            if (product is null)
            {
                _logger.LogWarning("No product {ProductId} found in the database.", productId);
                return NotFound(new { message = $"There is no product with ID {productId} in the database." });
            }
            
            _logger.LogInformation("Successfully retrieved product {ProductId}.", productId);
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
            _logger.LogInformation("Creating a product with name {ProductName}.", dto.ProductName);

            // Validate input
            if (string.IsNullOrWhiteSpace(dto.ProductName))
            {
                _logger.LogWarning("Product creation failed - Product name is required.");
                return BadRequest(new { message = "Product name is required." });
            }

            if (dto.Price <= 0)
            {
                _logger.LogWarning("Product creation failed - Price must be greater than 0. Provided: {Price}", dto.Price);
                return BadRequest(new { message = "Price must be greater than 0." });
            }

            if (dto.Stock < 0)
            {
                _logger.LogWarning("Product creation failed - Stock cannot be negative. Provided: {Stock}", dto.Stock);
                return BadRequest(new { message = "Stock cannot be negative." });
            }

            var existingProduct = await _dbContext.Products
                .FirstOrDefaultAsync(u => u.ProductName == dto.ProductName);

            if (existingProduct != null)
            {
                _logger.LogWarning("Product creation failed - Product name {ProductName} already exists.", dto.ProductName);
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
            _logger.LogInformation("Created product event for product {ProductId} is passed to the message bus.", product.ProductId);
            await _publishEndpoint.Publish(new ProductCreatedEvent(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock,
                product.CreatedAt
            ));

            _logger.LogInformation("Successfully created product {ProductId}.", product.ProductId);

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
            _logger.LogInformation("Updating product {ProductId}.", id);
            var product = await _dbContext.Products.FindAsync(id);

            if (product is null)
            {
                _logger.LogWarning("No product {ProductId} found in the database.", id);
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
                    _logger.LogWarning("Product update failed - Product name {ProductName} already exists.", dto.ProductName);
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
                _logger.LogWarning("Product update failed - Price must be greater than 0. Provided: {Price}", dto.Price.Value);
                return BadRequest(new { message = "Price must be greater than 0." });
            }

            if (dto.Stock.HasValue && dto.Stock.Value >= 0 && product.Stock != dto.Stock.Value)
            {
                product.Stock = dto.Stock.Value;
                hasChanges = true;
            }
            else if (dto.Stock.HasValue && dto.Stock.Value < 0)
            {
                _logger.LogWarning("Product update failed - Stock cannot be negative. Provided: {Stock}", dto.Stock.Value);
                return BadRequest(new { message = "Stock cannot be negative." });
            }

            if (hasChanges)
            {
                product.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // publish event UpdateProductEvent
                _logger.LogInformation("Updated product event for product {ProductId} is passed to the message bus.", product.ProductId);
                await _publishEndpoint.Publish(new ProductUpdatedEvent(
                    product.ProductId,
                    product.ProductName,
                    product.Price,
                    product.Stock,
                    product.UpdatedAt
                ));
                
                _logger.LogInformation("Successfully updated product {ProductId}.", product.ProductId);
            }
            else
            {
                _logger.LogInformation("No changes detected for product {ProductId}.", id);
            }

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
            _logger.LogInformation("Deleting product {ProductId}.", id);
            var product = await _dbContext.Products.FindAsync(id);

            if (product is null)
            {
                _logger.LogWarning("No product {ProductId} found in the database.", id);
                return NotFound(new { message = $"There is no product with ID {id} in the database." });
            }

            // Check if product is used in any orders (optional business rule)
            var hasOrders = await _dbContext.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
            {
                _logger.LogWarning("Product deletion failed - Product {ProductId} is referenced in existing orders.", id);
                return BadRequest(new { message = "Cannot delete product that has been ordered." });
            }

            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync();

            // publish event DeleteProductEvent
            _logger.LogInformation("Deleted product event for product {ProductId} is passed to the message bus.", product.ProductId);
            await _publishEndpoint.Publish(new ProductDeletedEvent(
                product.ProductId,
                product.ProductName,
                DateTime.UtcNow
            ));
            
            _logger.LogInformation("Successfully deleted product {ProductId}.", product.ProductId);
            
            return Ok(new { message = $"Product with ID {id} was deleted successfully." });

            // else Return Unathorized({message: "You are not authorized for this action."})
        }
    }
}
