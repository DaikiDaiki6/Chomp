using FinanceService.Attributes;
using FinanceService.Data;
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
        public PaymentController(
            ILogger<PaymentController> logger
        )
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPayments(int pageNumber, int pageSize)
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPaymentById(Guid id)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
