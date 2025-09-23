using System;
using FinanceService.Data;
using FinanceService.Models;
using Microsoft.EntityFrameworkCore;
using static FinanceService.DTO.WalletDto;

namespace FinanceService.Services;

public class WalletService
{
    private readonly ILogger<WalletService> _logger;
    private readonly FinanceDbContext _dbContext;

    public WalletService(ILogger<WalletService> logger,
        FinanceDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<List<WalletAdminDto>> GetAllWallets(int pageNumber, int pageSize)
    {
        _logger.LogInformation("Fetching all wallets. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        if (pageNumber < 1) throw new ArgumentException("Page number must be 1 or greater.", nameof(pageNumber));
        if (pageSize < 1 || pageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        var wallets = await _dbContext.Wallets
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} wallets from database", wallets.Count);

        return wallets.Select(p => new WalletAdminDto
        {
            WalletId = p.WalletId,
            CustomerId = p.CustomerId,
            Balance = p.Balance,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<object> GetWalletById(Guid id, Guid userId, string userRole)
    {
        _logger.LogInformation("Fetching wallet {WalletId} for user {UserId} with role {UserRole}", id, userId, userRole);

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(p =>
            p.WalletId == id && (userRole == "Admin" || p.CustomerId == userId));

        if (wallet is null)
        {
            var message = userRole == "Admin"
                ? $"There is no wallet with ID {id} in the database."
                : $"There is no wallet with ID {id} for this user in the database.";

            _logger.LogWarning("Wallet not found. {Message}", message);
            throw new KeyNotFoundException(message);
        }

        if (userRole == "Admin")
        {
            _logger.LogInformation("Admin accessed wallet {WalletId}", wallet.WalletId);
            return new WalletAdminDto
            {
                WalletId = wallet.WalletId,
                CustomerId = wallet.CustomerId,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt
            };
        }

        _logger.LogInformation("User {UserId} accessed their wallet {WalletId}", userId, wallet.WalletId);
        return new WalletUserDto
        {
            WalletId = wallet.WalletId,
            Balance = wallet.Balance,
            CreatedAt = wallet.CreatedAt
        };
    }

    public async Task<WalletUserDto?> GetWalletByCustomerId(Guid customerId)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.CustomerId == customerId);
        if (wallet is null)
            {
                throw new KeyNotFoundException("Wallet not found for this user.");
            }

            return new WalletUserDto
            {
                WalletId = wallet.WalletId,
                Balance = wallet.Balance,
                CreatedAt = wallet.CreatedAt
            };
    }

    public async Task WalletTopup(Guid userId, decimal amount)
    {
        _logger.LogInformation("User {UserId} requested top-up of {Amount}", userId, amount);

        if (amount <= 0)
        {
            _logger.LogWarning("Invalid top-up attempt by {UserId}. Amount: {Amount}", userId, amount);
            throw new ArgumentException("Top-up amount must be greater than 0");
        }

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.CustomerId == userId)
            ?? throw new KeyNotFoundException($"No wallet found for user {userId}");

        wallet.Balance += amount;

        _dbContext.Transactions.Add(new Transaction
        {
            TransactionId = Guid.NewGuid(),
            WalletId = wallet.WalletId,
            Amount = amount,
            TransactionType = TransactionType.Topup,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Top-up successful. User {UserId}, Wallet {WalletId}, Amount {Amount}, New Balance {Balance}",
            userId, wallet.WalletId, amount, wallet.Balance);
    }
}