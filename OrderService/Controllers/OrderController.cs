using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrdersDbContext _dbContext;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderController> _logger;
        public OrderController(OrdersDbContext dbContext, IPublishEndpoint publishEndpoint, ILogger<OrderController> logger)
        {
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        [HttpGet] // for Admin only
        public async Task<IActionResult> GetAll()
        {
            // check if loggedin 
            // if(User.Role == Admin) do this 
            // get logged in user ID then do this 
            _logger.LogInformation("Getting all orders - Admin Request");
            var allOrders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                //.Where(o => user.Id == o.CustomerId) 
                .ToListAsync();
            if (allOrders is null || allOrders.Count == 0)
            {
                _logger.LogWarning("No orders found in the database.");
                return NotFound(new { message = "There are no orders in the database." });
            }
            _logger.LogInformation("Successfully retrieved {UserCount} users.", allOrders.Count);
            return Ok(allOrders.Select(MapToGetOrderDto));
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            // check if logged in 
            // if user _logger.LogInformation("Getting user - User {id} Request", id);
            // if admin _logger.LogInformation("Getting user - Admin Request");
            _logger.LogInformation("Getting order - Admin Request for order {OrderId}", id); //temporary
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id); // and (o => userId == o.CustomerId)

            if (order is null)
            {
                _logger.LogWarning("No order {OrderId} found in the database.", id);
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }
            _logger.LogInformation("Successfully retrieved order {id}.", id);
            return Ok(MapToGetOrderDto(order));
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            // check if logged in
            _logger.LogInformation("Creating an order for customer {CustomerId}.", Guid.NewGuid()); // for now, get UserId for this later
            if (dto.OrderItems == null || dto.OrderItems.Count == 0)
            {
                _logger.LogWarning("Order creation failed - Order Items count in dto is 0, Order will not proceed.");
                return BadRequest(new { message = "Order must contain at least one item." });
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
                return BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
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

            if (stockErrors.Any())
            {
                _logger.LogWarning("Order creation failed - Insufficient stock for {ProductCount} products", stockErrors.Count);
                return BadRequest(new { 
                    message = "Insufficient stock for the following products:", 
                    stockErrors = stockErrors 
                });
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
                return BadRequest(new { message = "Order must contain at least one item with quantity greater than 0." });
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
            return CreatedAtAction(nameof(GetOrderById),
                new { id = order.OrderId },
                MapToGetOrderDto(createdOrder));

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}/confirm-order")]
        public async Task<IActionResult> ConfirmOrder(Guid id)
        {
            // check if logged in
            _logger.LogInformation("Confirming order {OrderId}.", id);

            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null)
            {
                _logger.LogWarning("No order {OrderId} found in the database.", id);
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }

            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning("Order confirmation failed - Order {OrderId} is already {Status}.", id, order.Status);
                return BadRequest(new { message = $"Order is already {order.Status}. Only pending orders can be confirmed." });
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
            return Ok(new
            {
                message = "Order is confirmed and is being processed by the payment service",
                order = MapToGetOrderDto(order)
            }
            );
        }

        /// <summary>
        /// Replace the entire order items with the provided list (smart merge).
        /// Items not in the request will be removed. Use this when you want the order to match exactly what you send.
        /// </summary>
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditOrder(Guid id, EditOrderDto dto)
        {
            // check if logged in
            _logger.LogInformation("Updating order {OrderId}.", id);
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null)
            {
                _logger.LogWarning("No order {OrderId} found in the database.", id);
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }

            bool hasChanges = false;

            if (dto.OrderItems != null && dto.OrderItems.Count > 0)
            {
                // Filter out items with null ProductId or Quantity, then get product IDs
                var validItems = dto.OrderItems.Where(item => item.ProductId.HasValue && item.Quantity.HasValue && item.Quantity.Value > 0).ToList();

                if (validItems.Count == 0)
                {
                    _logger.LogWarning("Order update failed - No valid order items provided for order {OrderId}.", id);
                    return BadRequest(new { message = "No valid order items provided. ProductId and Quantity (greater than 0) are required." });
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
                    return BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
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
                            existingItem.Quantity = itemDto.Quantity!.Value;
                            _logger.LogInformation("Performing smart merge for order {OrderId} with {ItemCount} items.", id, validItems.Count);
                            hasChanges = true;
                        }
                        if (existingItem.UnitPrice != product.Price)
                        {
                            existingItem.UnitPrice = product.Price;
                            _logger.LogInformation("Performing smart merge for order {OrderId} with {ItemCount} items.", id, validItems.Count);
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
                        _logger.LogInformation("Performing smart merge for order {OrderId} with {ItemCount} items.", id, validItems.Count);
                        hasChanges = true;
                    }
                }
            }

            if (!hasChanges)
            {
                _logger.LogInformation("No changes detected for order {OrderId}.", id);
                return Ok(MapToGetOrderDto(order));
            }

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
            return Ok(MapToGetOrderDto(updatedOrder));

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        /// <summary>
        /// Add new items to an existing order. If an item already exists, increases the quantity.
        /// </summary>
        [HttpPost("{id:guid}/items")]
        public async Task<IActionResult> AddOrderItems(Guid id, List<CreateOrderItemDto> newItems)
        {
            // check if logged in
            _logger.LogInformation("Adding items to order {OrderId}.", id);
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null)
            {
                _logger.LogWarning("No order {OrderId} found in the database.", id);
                return NotFound(new { message = $"There are no order with ID {id} in the database." });
            }

            if (newItems == null || newItems.Count == 0)
            {
                _logger.LogWarning("Add items failed - No items provided for order {OrderId}.", id);
                return BadRequest(new { message = "At least one item must be provided." });
            }

            // Filter valid items
            var validItems = newItems.Where(item => item.Quantity > 0).ToList();
            if (validItems.Count == 0)
            {
                _logger.LogWarning("Add items failed - All items have invalid quantities for order {OrderId}.", id);
                return BadRequest(new { message = "All items must have quantity greater than 0." });
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
                return BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
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
            return Ok(MapToGetOrderDto(updatedOrder));
        }

        /// <summary>
        /// Remove specific items from an order by product IDs.
        /// </summary>
        [HttpDelete("{id:guid}/items")]
        public async Task<IActionResult> RemoveOrderItems(Guid id, List<Guid> productIdsToRemove)
        {
            // check if logged in
            _logger.LogInformation("Removing items from order {OrderId}.", id);
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null)
            {
                _logger.LogWarning("No order {OrderId} found in the database.", id);
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }

            if (productIdsToRemove == null || productIdsToRemove.Count == 0)
            {
                _logger.LogWarning("Remove items failed - No product IDs provided for order {OrderId}.", id);
                return BadRequest(new { message = "At least one product ID must be provided." });
            }

            _logger.LogInformation("Attempting to remove {ProductCount} product(s) from order {OrderId}: {ProductIds}",
                productIdsToRemove.Count, id, string.Join(",", productIdsToRemove));

            // Find items to remove
            var itemsToRemove = order.OrderItems
                .Where(oi => productIdsToRemove.Contains(oi.ProductId))
                .ToList();

            if (itemsToRemove.Count == 0)
            {
                _logger.LogWarning("Remove items failed - No matching items found in order {OrderId} for product IDs: {ProductIds}",
                    id, string.Join(",", productIdsToRemove));
                return BadRequest(new { message = "No matching items found to remove." });
            }

            _logger.LogInformation("Found {ItemCount} items to remove from order {OrderId}.", itemsToRemove.Count, id);

            // Check if removing these items would leave the order empty
            var remainingItems = order.OrderItems.Count - itemsToRemove.Count;
            if (remainingItems == 0)
            {
                _logger.LogInformation("Removing all items from order {OrderId} - auto-deleting entire order.", id);
                // Auto-delete the entire order
                _dbContext.Orders.Remove(order);
                await _dbContext.SaveChangesAsync();

                // Publish OrderDeletedEvent
                _logger.LogInformation("Deleted order event for order {OrderId} is passed to the message bus.", id);
                await _publishEndpoint.Publish(new OrderCancelledEvent(
                    order.OrderId,
                    order.CustomerId,
                    "All items removed from order", // replace this with better reason after Payment service is added
                    DateTime.UtcNow
                ));
                _logger.LogInformation("Successfully deleted order {OrderId} (all items removed).", id);
                return Ok(new { message = $"Order with ID {id} is deleted successfully." });
            }

            // Remove only the specified items
            _logger.LogInformation("Removing {ItemCount} items from order {OrderId}, {RemainingCount} items will remain.",
                itemsToRemove.Count, id, remainingItems);
            _dbContext.OrderItems.RemoveRange(itemsToRemove);
            await _dbContext.SaveChangesAsync();

            // Return updated order
            var updatedOrder = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstAsync(o => o.OrderId == id);

            // publish event OrderUpdatedEvent
            _logger.LogInformation("Updated order event for order {OrderId} is passed to the message bus.", id);
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
            return Ok(MapToGetOrderDto(updatedOrder));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            // check if logged in
            _logger.LogInformation("Deleting order {OrderId}.", id);
            var order = await _dbContext.Orders.FindAsync(id);
            if (order is null)
            {
                _logger.LogWarning("No order {OrderId} found in the database.", id);
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();

            // Publish OrderDeletedEvent
            _logger.LogInformation("Deleted order event for order {OrderId} is passed to the message bus.", id);
            await _publishEndpoint.Publish(new OrderCancelledEvent(
                order.OrderId,
                order.CustomerId,
                "Reason", // replace this with better reason after Payment service is added
                DateTime.UtcNow
            ));

            _logger.LogInformation("Successfully deleted order {OrderId}.", id);
            return Ok(new { message = $"Order with ID {id} is deleted successfully." });

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        // Helper method to reduce code duplication
        private static GetOrderDto MapToGetOrderDto(Order order)
        {
            return new GetOrderDto(
                order.OrderId,
                order.CustomerId,
                order.TotalPrice,
                order.CreatedAt,
                order.Status,
                order.OrderItems.Select(e => new GetOrderItemDto(
                    e.OrderItemId,
                    e.Quantity,
                    e.UnitPrice,
                    new ProductDto(
                        e.Product.ProductId,
                        e.Product.ProductName,
                        e.Product.Price,
                        e.Product.Stock
                    ),
                    e.TotalPrice
                )).ToList()
            );
        }
    }
}
