using System;
using System.Linq.Expressions;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services.Interfaces;

namespace OrderService.Services;

public class ProductService : IProductService
{
    private readonly OrdersDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProductService> _logger;
    public ProductService(OrdersDbContext dbContext,
            IPublishEndpoint publishEndpoint,
            ILogger<ProductService> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(dto.ProductName))
        {
            throw new InvalidOperationException("Product name is required.");
        }

        if (dto.Price <= 0)
        {
            throw new InvalidOperationException("Price must be greater than 0.");
        }

        if (dto.Stock < 0)
        {
            throw new InvalidOperationException("Stock cannot be negative.");
        }

        var existingProduct = await _dbContext.Products
            .FirstOrDefaultAsync(u => u.ProductName == dto.ProductName);

        if (existingProduct != null)
        {
            throw new InvalidOperationException("Product already exists.");
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

        // Publish event
        await _publishEndpoint.Publish(new ProductCreatedEvent(
            product.ProductId,
            product.ProductName,
            product.Price,
            product.Stock,
            product.CreatedAt
        ));

        _logger.LogInformation("Successfully created product {ProductId}", product.ProductId);

        return ProductProjection.Compile()(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _dbContext.Products.FindAsync(id);

        if (product is null)
        {
            throw new KeyNotFoundException($"There is no product with ID {id} in the database.");
        }

        // Check if product is used in any orders (optional business rule)
        var hasOrders = await _dbContext.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
        {
            throw new InvalidOperationException("Cannot delete product that has been ordered.");
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        // Publish event
        await _publishEndpoint.Publish(new ProductDeletedEvent(
            product.ProductId,
            product.ProductName,
            DateTime.UtcNow
        ));

        _logger.LogInformation("Successfully deleted product {ProductId}", product.ProductId);
    }

    public async Task<ProductDto> EditProductAsync(Guid id, EditProductDto dto)
    {
        var product = await _dbContext.Products.FindAsync(id);

        if (product is null)
        {
            throw new KeyNotFoundException($"There is no product with ID {id} in the database.");
        }

        bool hasChanges = false;

        if (!string.IsNullOrWhiteSpace(dto.ProductName) && product.ProductName != dto.ProductName)
        {
            // Check if new name already exists
            var existingProduct = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.ProductName == dto.ProductName && p.ProductId != id);

            if (existingProduct != null)
            {
                throw new InvalidOperationException("Product name already exists.");
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
            throw new InvalidOperationException("Price must be greater than 0.");
        }

        if (dto.Stock.HasValue && dto.Stock.Value >= 0 && product.Stock != dto.Stock.Value)
        {
            product.Stock = dto.Stock.Value;
            hasChanges = true;
        }
        else if (dto.Stock.HasValue && dto.Stock.Value < 0)
        {
            throw new InvalidOperationException("Stock cannot be negative.");
        }

        if (hasChanges)
        {
            product.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // Publish event
            await _publishEndpoint.Publish(new ProductUpdatedEvent(
                product.ProductId,
                product.ProductName,
                product.Price,
                product.Stock,
                product.UpdatedAt
            ));
            
            _logger.LogInformation("Successfully updated product {ProductId}", product.ProductId);
        }

        return ProductProjection.Compile()(product);
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var allProducts = await _dbContext.Products
            .Select(ProductProjection)
            .ToListAsync();
        if (allProducts.Count == 0)
        {
            throw new KeyNotFoundException("There are no products in the database.");
        }
        return allProducts;
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid productId)
    {
        var product = await _dbContext.Products
            .Select(ProductProjection)
            .FirstOrDefaultAsync(o => o.ProductId == productId);
        if (product is null)
        {
            throw new KeyNotFoundException($"There is no product with ID {productId} in the database.");
        }
        return product;
    }

    private static readonly Expression<Func<Product, ProductDto>> ProductProjection = product => new ProductDto(
        product.ProductId,
        product.ProductName,
        product.Price,
        product.Stock
    );
}
