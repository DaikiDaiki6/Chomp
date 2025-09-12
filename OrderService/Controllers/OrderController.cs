using Microsoft.AspNetCore.Mvc;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IOrderService _orderService;
        
        public OrderController(
            ILogger<OrderController> logger,
            IOrderService orderService)
        {
            _logger = logger;
            _orderService = orderService;
        }

        [HttpGet] // for Admin only
        public async Task<IActionResult> GetAll()
        {
            // check if loggedin 
            // if(User.Role == Admin) do this 
            // get logged in user ID then do this 
            _logger.LogInformation("Getting all orders - Admin Request");
            try
            {
                var allOrders = await _orderService.GetAllAsync();
                return Ok(allOrders);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpGet("{id:guid}")] // mainly for Admin -- can be abused (User can use it too)
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            // check if logged in 
            // if user _logger.LogInformation("Getting user - User {id} Request", id);
            // if admin _logger.LogInformation("Getting user - Admin Request");
            _logger.LogInformation("Getting order - Admin Request for order {OrderId}", id); //temporary
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            // check if logged in
            _logger.LogInformation("Creating an order for customer {CustomerId}.", Guid.NewGuid()); // for now, get UserId for this later
            try
            {
                var createOrder = await _orderService.CreateOrderAsync(dto);

                _logger.LogInformation("Successfully created order {OrderId}.", createOrder.OrderId);
                return CreatedAtAction(nameof(GetOrderById),
                    new { id = createOrder.OrderId },
                    createOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }


            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}/confirm-order")]
        public async Task<IActionResult> ConfirmOrder(Guid id)
        {
            // check if logged in
            _logger.LogInformation("Confirming order {OrderId}.", id);
            try
            {
                var order = await _orderService.ConfirmOrderAsync(id);

                _logger.LogInformation("Successfully confirmed order {OrderId}.", order.OrderId);
                return Ok(new
                {
                    message = "Order is confirmed and is being processed by the payment service",
                    order = order
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
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
            try
            {
                var updatedOrder = await _orderService.EditOrderAsync(id, dto);
                return Ok(updatedOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
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
            try
            {
                var addedItems = await _orderService.AddOrderItemsAsync(id, newItems);
                return Ok(addedItems);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        /// <summary>
        /// Remove specific items from an order by product IDs.
        /// </summary>
        [HttpDelete("{id:guid}/items")]
        public async Task<IActionResult> RemoveOrderItems(Guid id, List<Guid> productIdsToRemove)
        {
            // check if logged in
            _logger.LogInformation("Removing items from order {OrderId}.", id);
            try
            {
                var orders = await _orderService.RemoveOrderItemsAsync(id, productIdsToRemove);
                return Ok(orders);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            // check if logged in
            _logger.LogInformation("DeleteOrder endpoint called for user: {UserId}", id);
            try
            {
                await _orderService.DeleteOrderAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }

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