using FinanceService.Attributes;
using FinanceService.Services;
using FinanceService.Services.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AccountStatusFilter]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly PaymentService _paymentService;

        public PaymentController(
            ILogger<PaymentController> logger,
            PaymentService paymentService
        )
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayments(int pageNumber, int pageSize)
        {
            _logger.LogInformation("Admin requested all payments. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var allPayments = await _paymentService.GetAllPayments(pageNumber, pageSize);

            _logger.LogInformation("Retrieved {Count} payments for admin", allPayments.Count);
            return Ok(allPayments);
        }

        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments(int pageNumber, int pageSize)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user token when requesting my-payments");
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            _logger.LogInformation("User {UserId} requested their payments. Page: {PageNumber}, Size: {PageSize}", userGuid, pageNumber, pageSize);

            var allPayments = await _paymentService.GetMyPayments(userGuid, pageNumber, pageSize);

            _logger.LogInformation("Retrieved {Count} payments for user {UserId}", allPayments.Count, userGuid);
            return Ok(allPayments);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user token when requesting payment {PaymentId}", id);
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("User {UserId} has no role when requesting payment {PaymentId}", userId, id);
                return Unauthorized(new { errorMessage = "User role is missing or invalid" });
            }

            _logger.LogInformation("User {UserId} with role {UserRole} requested payment {PaymentId}", userGuid, userRole, id);

            var payment = await _paymentService.GetPaymentById(id, userGuid, userRole);

            _logger.LogInformation("Payment {PaymentId} retrieved successfully for user {UserId}", id, userGuid);
            return Ok(payment);
        }
    }
}