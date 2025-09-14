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

public class OrderService : IOrderService
{
    private readonly OrdersDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrdersDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<List<GetOrderDto>> GetAllAsync()
    {
        var allOrders = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .ToListAsync();

        if (allOrders.Count == 0)
        {
            throw new KeyNotFoundException("There are no orders in the database.");
        }

        return allOrders.Select(MapToGetOrderDto).ToList();
    }

    public async Task<GetOrderDto> GetOrderByIdAsync(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id); //# and (o => userId == o.CustomerId) later on when log in is added

        if (order is null)
        {
            throw new KeyNotFoundException($"There is no order with ID {id} in the database.");
        }

        return MapToGetOrderDto(order);
    }

    public async Task<GetOrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        if (dto.OrderItems == null || dto.OrderItems.Count == 0)
        {
            _logger.LogWarning("Order creation failed - Order Items count in dto is 0, Order will not proceed.");
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        // Get product IDs from the order items
        var productIds = dto.OrderItems.Select(item => item.ProductId).ToList();

        // Fetch products from database to get current prices
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToListAsync();

        // Validate that all products exist
        if (products.Count != productIds.Count)
        {
            var foundProductIds = products.Select(p => p.ProductId).ToHashSet();
            var missingProductIds = productIds.Where(id => !foundProductIds.Contains(id));
            _logger.LogWarning("Product(s) in the order not found in the Product Database: {productIds}", string.Join(",", missingProductIds));
            throw new KeyNotFoundException($"Products not found: {string.Join(", ", missingProductIds)}");

        }

        // Validate stock availability
        var stockErrors = new List<string>();
        foreach (var item in dto.OrderItems)
        {
            var product = products.First(p => p.ProductId == item.ProductId);
            if (product.Stock < item.Quantity)
            {
                stockErrors.Add($"Product '{product.ProductName}': Requested {item.Quantity}, Available {product.Stock}");
                _logger.LogWarning("Stock validation failed - Product {ProductId} ({ProductName}): Requested {RequestedQuantity}, Available {AvailableStock}",
                    product.ProductId, product.ProductName, item.Quantity, product.Stock);
            }
        }

        if (stockErrors.Count != 0)
        {
            _logger.LogWarning("Order creation failed - Insufficient stock for {ProductCount} products", stockErrors.Count);
            throw new InvalidOperationException($"Insufficient stock for the following products: {string.Join(",", stockErrors)}");
        }

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(), // will change this to UserId later when I add log in function
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderItems = dto.OrderItems.Select(item =>
            {
                var product = products.First(p => p.ProductId == item.ProductId);
                return new Models.OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price // Use current product price
                };
            })
            .Where(item => item.Quantity > 0)
            .ToList()
        };

        // Check if we have any valid items after filtering
        if (order.OrderItems.Count == 0)
        {
            _logger.LogWarning("Order creation failed - Order Items count in order is 0, Order will not proceed.");
            throw new InvalidOperationException("Order must contain at least one item with quantity greater than 0.");
        }

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // Load the created order with includes for response
        var createdOrder = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .FirstAsync(o => o.OrderId == order.OrderId);

        // publish event CreateOrderEvent
        _logger.LogInformation("Created order event for order {id} is passed to the message bus.", order.OrderId);
        await _publishEndpoint.Publish(new OrderPlacedEvent(
            createdOrder.OrderId,
            createdOrder.CustomerId,
            createdOrder.TotalPrice,
            createdOrder.CreatedAt,
            createdOrder.OrderItems.Select(o => new Contracts.OrderItem(
                o.OrderItemId,
                o.ProductId,
                o.Product.ProductName,
                o.Quantity,
                o.UnitPrice
            )).ToList()
        ));

        _logger.LogInformation("Successfully created order {OrderId}.", order.OrderId);
        return MapToGetOrderDto(createdOrder);

    }

    public async Task<GetOrderDto> ConfirmOrderAsync(Guid id)
    {
        var order = await _dbContext.Orders
                 .Include(o => o.OrderItems)
                 .ThenInclude(o => o.Product)
                 .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            _logger.LogWarning("No order {OrderId} found in the database.", id);
            throw new KeyNotFoundException($"There is no order with ID {id} in the database.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Order confirmation failed - Order {OrderId} is already {Status}.", id, order.Status);
            throw new InvalidOperationException($"Order is already {order.Status}. Only pending orders can be confirmed.");
        }
        _logger.LogInformation("Order {OrderId} total price: ${TotalPrice}", id, order.TotalPrice);
        order.Status = OrderStatus.Confirmed;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order confirmed event for order {OrderId} is passed to the message bus.", order.OrderId);
        await _publishEndpoint.Publish(new OrderConfirmedEvent(
            order.OrderId,
            order.CustomerId,
            order.TotalPrice,
            DateTime.UtcNow
        ));
        _logger.LogInformation("Successfully confirmed order {OrderId}.", order.OrderId);
        return MapToGetOrderDto(order);
    }

    public async Task<GetOrderDto> EditOrderAsync(Guid id, EditOrderDto dto)
    {
        var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            _logger.LogWarning("No order {OrderId} found in the database.", id);
            throw new KeyNotFoundException($"There is no order with ID {id} in the database.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Order Edit failed - Order {OrderId} is already {Status}.", id, order.Status);
            throw new InvalidOperationException($"Order is already {order.Status}. Only pending orders can be edited.");
        }

        bool hasChanges = false;

        if (dto.OrderItems != null && dto.OrderItems.Count > 0)
        {
            // Filter out items with null ProductId or Quantity, then get product IDs
            var validItems = dto.OrderItems.Where(item => item.ProductId.HasValue && item.Quantity.HasValue && item.Quantity.Value > 0).ToList();

            if (validItems.Count == 0)
            {
                _logger.LogWarning("Order update failed - No valid order items provided for order {OrderId}.", id);
                throw new InvalidOperationException("No valid order items provided. ProductId and Quantity (greater than 0) are required.");
            }

            var productIds = validItems.Select(item => item.ProductId!.Value).ToList();

            // Fetch products from database to get current prices
            var products = await _dbContext.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            // Validate that all products exist
            if (products.Count != productIds.Count)
            {
                var foundProductIds = products.Select(p => p.ProductId).ToHashSet();
                var missingProductIds = productIds.Where(id => !foundProductIds.Contains(id));
                _logger.LogWarning("Order update failed - Products not found in database: {ProductIds}", string.Join(",", missingProductIds));
                throw new InvalidOperationException($"Products not found: {string.Join(", ", missingProductIds)}");
            }

            // Smart merge: Treat request as desired final state
            var requestedProductIds = validItems.Select(item => item.ProductId!.Value).ToHashSet();

            // Step 1: Remove items not in the request (items being removed)
            var itemsToRemove = order.OrderItems.Where(item => !requestedProductIds.Contains(item.ProductId)).ToList();
            foreach (var item in itemsToRemove)
            {
                _dbContext.OrderItems.Remove(item);
                hasChanges = true;
            }

            // Step 2: Update existing items and add new ones
            foreach (var itemDto in validItems)
            {
                var existingItem = order.OrderItems.FirstOrDefault(item => item.ProductId == itemDto.ProductId!.Value);
                var product = products.First(p => p.ProductId == itemDto.ProductId!.Value);

                if (existingItem != null)
                {
                    // Update existing item if quantity or price changed
                    if (existingItem.Quantity != itemDto.Quantity!.Value)
                    {
                        _logger.LogDebug("Updated quantity for product {ProductId} in order {OrderId}: {OldQuantity} -> {NewQuantity}",
                            itemDto.ProductId, id, existingItem.Quantity, itemDto.Quantity.Value);
                        existingItem.Quantity = itemDto.Quantity!.Value;
                        hasChanges = true;
                    }
                    if (existingItem.UnitPrice != product.Price)
                    {
                        _logger.LogDebug("Updated price for product {ProductId} in order {OrderId}: {OldPrice} -> {NewPrice}",
                            itemDto.ProductId, id, existingItem.UnitPrice, product.Price);
                        existingItem.UnitPrice = product.Price;
                        hasChanges = true;
                    }
                }
                else
                {
                    // Add new item
                    var newItem = new Models.OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        ProductId = itemDto.ProductId!.Value,
                        Quantity = itemDto.Quantity!.Value,
                        UnitPrice = product.Price
                    };
                    _dbContext.OrderItems.Add(newItem);
                    _logger.LogDebug("Added new product {ProductId} to order {OrderId} with quantity {Quantity}",
                        itemDto.ProductId, id, itemDto.Quantity);
                    hasChanges = true;
                }
            }
        }

        if (!hasChanges)
        {
            _logger.LogInformation("No changes detected for order {OrderId}.", id);
            return MapToGetOrderDto(order);
        }

        _logger.LogInformation("Performing smart merge for order {OrderId}.", id);
        await _dbContext.SaveChangesAsync();

        // Reload the order with fresh data for response
        var updatedOrder = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .FirstAsync(o => o.OrderId == id);

        // publish event OrderUpdatedEvent
        _logger.LogInformation("Updated order event for order {OrderId} is passed to the message bus.", updatedOrder.OrderId);
        await _publishEndpoint.Publish(new OrderUpdatedEvent(
            updatedOrder.OrderId,
            updatedOrder.CustomerId,
            updatedOrder.TotalPrice,
            updatedOrder.CreatedAt,
            updatedOrder.OrderItems.Select(o => new Contracts.OrderItem(
                o.OrderItemId,
                o.ProductId,
                o.Product.ProductName,
                o.Quantity,
                o.UnitPrice
            )).ToList()
        ));

        _logger.LogInformation("Successfully updated order {OrderId}.", id);
        return MapToGetOrderDto(updatedOrder);
    }

    public async Task<GetOrderDto> AddOrderItemsAsync(Guid id, List<CreateOrderItemDto> newItems)
    {
        var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            _logger.LogWarning("No order {OrderId} found in the database.", id);
            throw new KeyNotFoundException($"There is no order with ID {id} in the database.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Order Item Addition failed - Order {OrderId} is already {Status}.", id, order.Status);
            throw new InvalidOperationException($"Order is already {order.Status}. Only pending orders can be modified.");
        }

        if (newItems == null || newItems.Count == 0)
        {
            _logger.LogWarning("Add items failed - No items provided for order {OrderId}.", id);
            throw new InvalidOperationException("At least one item must be provided.");
        }

        // Filter valid items
        var validItems = newItems.Where(item => item.Quantity > 0).ToList();
        if (validItems.Count == 0)
        {
            _logger.LogWarning("Add items failed - All items have invalid quantities for order {OrderId}.", id);
            throw new InvalidOperationException("All items must have quantity greater than 0.");
        }

        _logger.LogInformation("Processing {ItemCount} valid items to add to order {OrderId}.", validItems.Count, id);

        var productIds = validItems.Select(item => item.ProductId).ToList();

        // Fetch products to get current prices
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToListAsync();

        // Validate that all products exist
        if (products.Count != productIds.Count)
        {
            var foundProductIds = products.Select(p => p.ProductId).ToHashSet();
            var missingProductIds = productIds.Where(id => !foundProductIds.Contains(id));
            _logger.LogWarning("Add items failed - Products not found in database: {ProductIds}", string.Join(",", missingProductIds));
            throw new InvalidOperationException($"Products not found: {string.Join(", ", missingProductIds)}");
        }

        // Add new items or update existing ones
        int itemsAdded = 0;
        int itemsUpdated = 0;
        foreach (var itemDto in validItems)
        {
            var existingItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == itemDto.ProductId);
            var product = products.First(p => p.ProductId == itemDto.ProductId);

            if (existingItem != null)
            {
                // Update quantity of existing item
                var oldQuantity = existingItem.Quantity;
                existingItem.Quantity += itemDto.Quantity;
                existingItem.UnitPrice = product.Price; // Update to current price
                _logger.LogDebug("Updated existing item {ProductId} in order {OrderId}: quantity {OldQuantity} -> {NewQuantity}",
                    itemDto.ProductId, id, oldQuantity, existingItem.Quantity);
                itemsUpdated++;
            }
            else
            {
                // Add new item
                _dbContext.OrderItems.Add(new Models.OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = id,
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price
                });
                _logger.LogDebug("Added new item {ProductId} to order {OrderId} with quantity {Quantity}",
                    itemDto.ProductId, id, itemDto.Quantity);
                itemsAdded++;
            }
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully added/updated items in order {OrderId}: {ItemsAdded} new, {ItemsUpdated} updated.",
            id, itemsAdded, itemsUpdated);

        // Return updated order
        var updatedOrder = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .FirstAsync(o => o.OrderId == id);

        // publish event OrderUpdatedEvent
        _logger.LogInformation("Updated order event for order {OrderId} is passed to the message bus.", updatedOrder.OrderId);
        await _publishEndpoint.Publish(new OrderUpdatedEvent(
            updatedOrder.OrderId,
            updatedOrder.CustomerId,
            updatedOrder.TotalPrice,
            updatedOrder.CreatedAt,
            updatedOrder.OrderItems.Select(o => new Contracts.OrderItem(
                o.OrderItemId,
                o.ProductId,
                o.Product.ProductName,
                o.Quantity,
                o.UnitPrice
            )).ToList()
        ));
        _logger.LogInformation("Successfully updated order {OrderId}.", id);
        return MapToGetOrderDto(updatedOrder);
    }

    public async Task<GetOrderDto> RemoveOrderItemsAsync(Guid id, List<RemoveOrderItemDto> itemsToRemove)
    {
        var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null)
        {
            _logger.LogWarning("No order {OrderId} found in the database.", id);
            throw new KeyNotFoundException($"There is no order with ID {id} in the database.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Order Item Removing failed - Order {OrderId} is already {Status}.", id, order.Status);
            throw new InvalidOperationException($"Order is already {order.Status}. Only pending orders can be modified.");
        }

        if (itemsToRemove == null || itemsToRemove.Count == 0)
        {
            _logger.LogWarning("Remove items failed - No items provided for order {OrderId}.", id);
            throw new InvalidOperationException("At least one item must be provided.");
        }

        // Filter valid items (quantity > 0)
        var validItems = itemsToRemove.Where(item => item.Quantity > 0).ToList();
        if (validItems.Count == 0)
        {
            _logger.LogWarning("Remove items failed - All items have invalid quantities for order {OrderId}.", id);
            throw new InvalidOperationException("All items must have quantity greater than 0.");
        }

        _logger.LogInformation("Attempting to remove quantities from {ProductCount} product(s) in order {OrderId}",
            validItems.Count, id);

        var orderItemsToCompletelyRemove = new List<Models.OrderItem>();
        bool hasChanges = false;

        foreach (var itemToRemove in validItems)
        {
            var existingOrderItem = order.OrderItems
                .FirstOrDefault(oi => oi.ProductId == itemToRemove.ProductId);

            if (existingOrderItem == null)
            {
                _logger.LogWarning("Product {ProductId} not found in order {OrderId} - skipping",
                    itemToRemove.ProductId, id);
                continue;
            }

            if (itemToRemove.Quantity >= existingOrderItem.Quantity)
            {
                // Remove entire item if quantity to remove >= current quantity
                _logger.LogInformation("Removing entire order item for product {ProductId} from order {OrderId} (requested: {RequestedQuantity}, current: {CurrentQuantity})",
                    itemToRemove.ProductId, id, itemToRemove.Quantity, existingOrderItem.Quantity);
                orderItemsToCompletelyRemove.Add(existingOrderItem);
                hasChanges = true;
            }
            else
            {
                // Deduct quantity
                var oldQuantity = existingOrderItem.Quantity;
                existingOrderItem.Quantity -= itemToRemove.Quantity;
                _logger.LogInformation("Reduced quantity for product {ProductId} in order {OrderId}: {OldQuantity} -> {NewQuantity}",
                    itemToRemove.ProductId, id, oldQuantity, existingOrderItem.Quantity);
                hasChanges = true;
            }
        }

        if (!hasChanges)
        {
            _logger.LogWarning("No valid items found to remove from order {OrderId}", id);
            throw new InvalidOperationException("No matching items found to remove.");
        }

        // Remove items that need to be completely removed
        if (orderItemsToCompletelyRemove.Count > 0)
        {
            _dbContext.OrderItems.RemoveRange(orderItemsToCompletelyRemove);
        }

        // Check if order would be empty after changes
        var remainingItemsCount = order.OrderItems.Count - orderItemsToCompletelyRemove.Count;
        var hasItemsWithQuantity = order.OrderItems
            .Where(oi => !orderItemsToCompletelyRemove.Contains(oi))
            .Any(oi => oi.Quantity > 0);

        if (remainingItemsCount == 0 || !hasItemsWithQuantity)
        {
            _logger.LogInformation("Removing all items from order {OrderId} - auto-deleting entire order.", id);

            // Auto-delete the entire order
            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();

            // Publish OrderCancelledEvent
            await _publishEndpoint.Publish(new OrderCancelledEvent(
                order.OrderId,
                order.CustomerId,
                "All items removed from order",
                DateTime.UtcNow
            ));

            _logger.LogInformation("Successfully deleted order {OrderId} (all items removed).", id);
            return MapToGetOrderDto(order);
        }

        // Save partial changes
        await _dbContext.SaveChangesAsync();

        // Return updated order
        var updatedOrder = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(o => o.Product)
            .FirstAsync(o => o.OrderId == id);

        // Publish OrderUpdatedEvent
        await _publishEndpoint.Publish(new OrderUpdatedEvent(
            updatedOrder.OrderId,
            updatedOrder.CustomerId,
            updatedOrder.TotalPrice,
            updatedOrder.CreatedAt,
            updatedOrder.OrderItems.Select(o => new Contracts.OrderItem(
                o.OrderItemId,
                o.ProductId,
                o.Product.ProductName,
                o.Quantity,
                o.UnitPrice
            )).ToList()
        ));

        _logger.LogInformation("Successfully updated order {OrderId}.", id);
        return MapToGetOrderDto(updatedOrder);
    }

    public async Task DeleteOrderAsync(Guid id)
    {
        var order = await _dbContext.Orders.FindAsync(id);

        if (order is null)
        {
            _logger.LogWarning("No order {OrderId} found in the database.", id);
            throw new KeyNotFoundException($"There is no order with ID {id} in the database.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogWarning("Order deletion failed - Order {OrderId} is already {Status}.", id, order.Status);
            throw new InvalidOperationException($"Order is already {order.Status}. Only pending orders can be deleted.");
        }

        _dbContext.Orders.Remove(order);
        await _dbContext.SaveChangesAsync();

        // Publish OrderDeletedEvent
        _logger.LogInformation("Deleted order event for order {OrderId} is passed to the message bus.", id);
        await _publishEndpoint.Publish(new OrderCancelledEvent(
            order.OrderId,
            order.CustomerId,
            "Order deleted by user request",
            DateTime.UtcNow
        ));

        _logger.LogInformation("Successfully deleted order {OrderId}.", id);
    }

    private static GetOrderDto MapToGetOrderDto(Order order)
    {
        var orderItems = order.OrderItems.Select(oi => new GetOrderItemDto(
            oi.OrderItemId,
            oi.Quantity,
            oi.UnitPrice,
            new ProductDto(
                oi.Product.ProductId,
                oi.Product.ProductName,
                oi.Product.Price,
                oi.Product.Stock
            ),
            oi.UnitPrice * oi.Quantity // TotalPrice
        )).ToList();

        return new GetOrderDto(
            order.OrderId,
            order.CustomerId,
            order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity), // TotalPrice
            order.CreatedAt,
            order.UpdatedAt,
            order.Status,
            orderItems
        );
    }
}
