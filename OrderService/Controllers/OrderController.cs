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
            return Ok(allOrders.Select(order => new GetOrderDto(
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
            )));
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
            return Ok(new GetOrderDto(
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

            ));
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            // check if logged in
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                CustomerId = dto.CustomerId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderItems = dto.OrderItems.Select(item => new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
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
                new GetOrderDto(
                 createdOrder.OrderId,
                 createdOrder.CustomerId,
                 createdOrder.TotalPrice,
                 createdOrder.CreatedAt,
                 createdOrder.Status,
                 createdOrder.OrderItems.Select(e => new GetOrderItemDto(
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
                ));

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
                order.OrderItems.Clear();
                foreach (var itemDto in dto.OrderItems)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = order.OrderId,
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice,
                    });
                }
                
                hasChanges = true;
            }

            if (!hasChanges)
            {
                return Ok(new GetOrderDto(
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
                ));
            }
            await _dbContext.SaveChangesAsync();

            return Ok(new GetOrderDto(
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
            ));

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
    }
}
