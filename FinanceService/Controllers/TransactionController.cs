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
    public class TransactionController : ControllerBase
    {
        private readonly ILogger<TransactionController> _logger;
        private readonly TransactionService _transactionService;
        public TransactionController(
            ILogger<TransactionController> logger,
            TransactionService transactionService
        )
        {
            _logger = logger;
            _transactionService = transactionService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(int pageNumber, int pageSize)
        {
            _logger.LogInformation("Admin requested transaction list. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

            var transactions = await _transactionService.GetAllTransactions(pageNumber, pageSize);

            _logger.LogInformation("Returned {Count} transactions for Admin request", transactions.Count);

            return Ok(transactions);
        }

        [HttpGet("{id:guid}")] 
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            _logger.LogInformation("Fetching transaction {transactionId}", id);

            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Unauthorized access attempt with invalid token for transaction {transactionId}", id);
                return Unauthorized(new { errorMessage = "Invalid user token" });
            }

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("Unauthorized access attempt with missing role for transaction {transactionId}", id);
                return Unauthorized(new { errorMessage = "User role is missing or invalid" });
            }

            var transaction = await _transactionService.GetTransactionById(id, userGuid, userRole);

            _logger.LogInformation("Transaction {transactionId} successfully retrieved by {UserRole}", id, userRole);

            return Ok(transaction);
        }
    }
}
