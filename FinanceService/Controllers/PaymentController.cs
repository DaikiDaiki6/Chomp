using FinanceService.Attributes;
using FinanceService.Data;
using FinanceService.Services;
using FinanceService.Services.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);
            try
            {
                var allPayments = await _paymentService.GetAllPayments(pageNumber, pageSize);
                return Ok(allPayments);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorMessage = ex.Message});
            }
        }

        [HttpGet("my-payments")]
        public async Task<IActionResult> GetMyPayments(int pageNumber, int pageSize)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new { errorMessage = "Invalid user token"});
            }

            try
            {
                var allPayments = await _paymentService.GetMyPayments(userGuid, pageNumber, pageSize);
                return Ok(allPayments);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorMessage = ex.Message});
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new { errorMessage = "Invalid user token"});
            }
            if (string.IsNullOrEmpty(userRole))
            {
                return Unauthorized(new { errorMessage = "User role is missing or invalid"});
            }

            try
            {
                var payment = await _paymentService.GetPaymentById(id, userGuid, userRole);
                return Ok(payment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message});
            }
        }
    }
}
