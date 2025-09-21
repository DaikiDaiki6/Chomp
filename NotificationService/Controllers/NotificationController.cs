using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Attributes;

namespace NotificationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AccountStatusFilter]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            ILogger<NotificationController> logger
           )
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(int pageNumber, int pageSize)
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> NotifRead(Guid id)
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
