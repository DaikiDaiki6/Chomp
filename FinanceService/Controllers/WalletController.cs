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
    public class WalletController : ControllerBase
    {
        private readonly ILogger<WalletController> _logger;
        public WalletController(
            ILogger<WalletController> logger
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
        public async Task<IActionResult> GetWalletById(Guid id)
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpPost("topup")]
        public async Task<IActionResult> WalletTopup(decimal topup)
        {
            // also validate in service
            await Task.CompletedTask;
            return Ok();
        }
    }
}
