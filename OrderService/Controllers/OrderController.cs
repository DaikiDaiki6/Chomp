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
        public OrderController(OrdersDbContext dbContext)
        {
            _dbContext = dbContext;
            //Serilog
        }

        [HttpGet] // for Admin only
        public async Task<IActionResult> GetAll()
        {
            // check if loggedin 
            // if(User.Role == Admin) do this 
            // get logged in user ID then do this 
            var allOrders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                //.Where(o => user.Id == o.CustomerId) 
                .ToListAsync();
            if (allOrders is null || allOrders.Count == 0)
            {
                return NotFound(new { message = "There are no orders in the database." });
            }
            return Ok(allOrders.Select(MapToGetOrderDto));
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpGet("{orderId:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            // check if logged in 
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId); // and (o => userId == o.CustomerId)
            if (order is null)
            {
                return NotFound(new { message = $"There is no order with ID {orderId} in the database." });
            }
            return Ok(MapToGetOrderDto(order));
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            // check if logged in

            if (dto.OrderItems == null || dto.OrderItems.Count == 0)
            {
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
                return BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
            }

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderItems = dto.OrderItems.Select(item =>
                {
                    var product = products.First(p => p.ProductId == item.ProductId);
                    return new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price // Use current product price
                    };
                }).ToList()
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            // Load the created order with includes for response
            var createdOrder = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstAsync(o => o.OrderId == order.OrderId);

            return CreatedAtAction(nameof(GetOrderById),
                new { orderId = order.OrderId },
                MapToGetOrderDto(createdOrder));

            // publish event CreateOrderEvent

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditOrder(Guid id, EditOrderDto dto)
        {
            // check if logged in
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order is null)
            {
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }

            bool hasChanges = false;

            if (dto.OrderItems != null && dto.OrderItems.Count > 0)
            {
                // Filter out items with null ProductId or Quantity, then get product IDs
                var validItems = dto.OrderItems.Where(item => item.ProductId.HasValue && item.Quantity.HasValue).ToList();

                if (validItems.Count == 0)
                {
                    return BadRequest(new { message = "No valid order items provided. ProductId and Quantity are required." });
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
                    return BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
                }

                order.OrderItems.Clear();
                foreach (var itemDto in validItems)
                {
                    var product = products.First(p => p.ProductId == itemDto.ProductId!.Value);
                    order.OrderItems.Add(new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        ProductId = itemDto.ProductId!.Value,
                        Quantity = itemDto.Quantity!.Value,
                        UnitPrice = product.Price, // Use current product price
                    });
                }

                hasChanges = true;
            }

            if (!hasChanges)
            {
                return Ok(MapToGetOrderDto(order));
            }
            await _dbContext.SaveChangesAsync();

            return Ok(MapToGetOrderDto(order));

            // publish event UpdateOrderEvent

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            // check if logged in
            var order = await _dbContext.Orders.FindAsync(id);
            if (order is null)
            {
                return NotFound(new { message = $"There is no order with ID {id} in the database." });
            }

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = $"Order with ID {id} is deleted successfully." });
            // publish event DeleteOrderEvent

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
