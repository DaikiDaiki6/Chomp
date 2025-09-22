using FinanceService.Attributes;
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
    public class WalletController : ControllerBase
    {
        private readonly ILogger<WalletController> _logger;
        private readonly WalletService _walletService;

        public WalletController(
            ILogger<WalletController> logger,
            WalletService walletService
        )
        {
            _logger = logger;
            _walletService = walletService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int pageNumber, int pageSize)
        {
            _logger.LogInformation("Admin requested wallet list. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var wallets = await _walletService.GetAllWallets(pageNumber, pageSize);

            _logger.LogInformation("Returned {Count} wallets for Admin request", wallets.Count);

            return Ok(wallets);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWalletById(Guid id)
        {
            _logger.LogInformation("Fetching wallet {WalletId}", id);

            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Unauthorized access attempt with invalid token for wallet {WalletId}", id);
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthorized access attempt with missing role for wallet {WalletId}", id);
                return Unauthorized(new { errorMessage = "User role is missing or invalid" });
            }

            var wallet = await _walletService.GetWalletById(id, userGuid, userRole);

            _logger.LogInformation("Wallet {WalletId} successfully retrieved by {UserRole}", id, userRole);

            return Ok(wallet);
        }

        [HttpPost("topup")]
        public async Task<IActionResult> WalletTopup(decimal topup)
        {
            _logger.LogInformation("User requested top-up of {Amount}", topup);

            var (userId, _, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Unauthorized top-up attempt with invalid token. Amount: {Amount}", topup);
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            await _walletService.WalletTopup(userGuid, topup);

            _logger.LogInformation("Top-up successful for user {UserId}. Amount: {Amount}", userGuid, topup);

            return Ok(new { message = "Top-up successful", topup });
        }
    }
}