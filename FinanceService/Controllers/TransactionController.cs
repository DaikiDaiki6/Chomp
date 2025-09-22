using FinanceService.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AccountStatusFilter]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ILogger<TransactionController> _logger;
        public TransactionController(
            ILogger<TransactionController> logger
        )
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
