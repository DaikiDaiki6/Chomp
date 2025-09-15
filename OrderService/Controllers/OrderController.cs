using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Attributes;
using OrderService.DTO;
using OrderService.Services.Helper;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers
{ // fix userGuid for editing only your orders and not others
    [Route("api/[controller]")]
    [ApiController]
    [AccountStatusFilter]
    [Authorize]
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int pageNumber, int pageSize)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            _logger.LogInformation("GetAll Endpoint - {Role} Request {UserID}", userRole, userId);
            try
            {
                var allOrders = await _orderService.GetAllAsync(pageNumber, pageSize);
                return Ok(allOrders);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders(int pageNumber, int pageSize)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("GetMyOrders Endpoint - {Role} Request {UserID}", userRole, userId);
            try
            {
                var userOrders = await _orderService.GetOrdersByUserIdAsync(userGuid, pageNumber, pageSize);
                return Ok(userOrders);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var (userId, userRole, isAdmin) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("GetOrderById Endpoint - {Role} Request {UserID} for Order {OrderID}",
                userRole, userId, id);
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id, userGuid, userRole ?? "User");
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("CreatingOrder Endpoint - User ID: {CustomerId}.", userId);
            try
            {
                var createOrder = await _orderService.CreateOrderAsync(dto, userGuid, userRole ?? "User");

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
        }

        [HttpPatch("{id:guid}/confirm-order")]
        public async Task<IActionResult> ConfirmOrder(Guid id)
        {
            var (userId, userRole, isAdmin) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("ConfirmOrder Endpoint - Order {OrderId} by User {UserId}", id, userId);

            try
            {
                var order = await _orderService.ConfirmOrderAsync(id, userGuid, userRole ?? "User");

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

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditOrder(Guid id, EditOrderDto dto)
        {
            var (userId, userRole, isAdmin) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

             _logger.LogInformation("EditOrder Endpoint - Order {OrderId} by User {UserId}", id, userId);
            try
            {
                var updatedOrder = await _orderService.EditOrderAsync(id, dto, userGuid, userRole ?? "User");
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
        }

        [HttpPost("{id:guid}/items")]
        public async Task<IActionResult> AddOrderItems(Guid id, List<CreateOrderItemDto> newItems)
        {
            var (userId, userRole, isAdmin) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("AddOrderItems Endpoint - Order {OrderId} by User {UserId}", id, userId);
            try
            {
                var addedItems = await _orderService.AddOrderItemsAsync(id, newItems, userGuid, userRole ?? "User");
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

        [HttpDelete("{id:guid}/items")]
        public async Task<IActionResult> RemoveOrderItems(Guid id, List<RemoveOrderItemDto> itemsToRemove)
        {
            var (userId, userRole, isAdmin) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

             _logger.LogInformation("RemoveOrderItems Endpoint - Order {OrderId} by User {UserId}", id, userId);
            try
            {
                var orders = await _orderService.RemoveOrderItemsAsync(id, itemsToRemove, userGuid, userRole ?? "User");
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
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized("Invalid user token");
            }

            _logger.LogInformation("DeleteOrder Endpoint - Order ID: {OrderId} from User ID:{UserId}.", id, userId);
            try
            {
                await _orderService.DeleteOrderAsync(id, userGuid, userRole ?? "User");
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
        }
    }
}