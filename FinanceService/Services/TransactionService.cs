using System;
using FinanceService.Data;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using static FinanceService.DTO.TransactionDto;

namespace FinanceService.Services;

public class TransactionService
{
    private readonly ILogger<TransactionService> _logger;
    private readonly FinanceDbContext _dbContext;

    public TransactionService(ILogger<TransactionService> logger,
        FinanceDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<List<TransactionAdminDto>> GetAllTransactions(int pageNumber, int pageSize)
    {
        _logger.LogInformation("Fetching all transactions. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        if (pageNumber < 1) throw new ArgumentException("Page number must be 1 or greater.", nameof(pageNumber));
        if (pageSize < 1 || pageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} transactions from database", transactions.Count);

        return transactions.Select(p => new TransactionAdminDto
        {
            TransactionId = p.TransactionId,
            WalletId = p.WalletId,
            Amount = p.Amount,
            TransactionType = p.TransactionType,
            RelatedOrderId = p.RelatedOrderId,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<List<TransactionUserDto>> GetAllMyTransactions(int pageNumber, int pageSize, Guid userId)
    {
        _logger.LogInformation("Fetching all transactions. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        if (pageNumber < 1) throw new ArgumentException("Page number must be 1 or greater.", nameof(pageNumber));
        if (pageSize < 1 || pageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(u => u.CustomerId == userId) ?? throw new KeyNotFoundException($"User {userId} does not have a wallet.");
        var walletId = wallet.WalletId;

        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(u => u.WalletId == walletId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        _logger.LogInformation("Retrieved {Count} transactions from database", transactions.Count);

        return transactions.Select(p => new TransactionUserDto
        {
            TransactionId = p.TransactionId,
            Amount = p.Amount,
            TransactionType = p.TransactionType,
            RelatedOrderId = p.RelatedOrderId,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<object> GetTransactionById(Guid id, Guid userId, string userRole)
    {
        _logger.LogInformation("Fetching transactions {TransactionId} for user {UserId} with role {UserRole}", id, userId, userRole);

        Guid? walletId = null;

        if (userRole != "Admin")
        {
            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(p => p.CustomerId == userId);
            if (wallet == null)
            {
                _logger.LogWarning("User {UserId} attempted to fetch transaction {TransactionId}, but no wallet exists", userId, id);
                throw new KeyNotFoundException($"No wallet found for user {userId}");
            }
            walletId = wallet.WalletId;
        }

        var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(p =>
            p.TransactionId == id && (userRole == "Admin" || p.WalletId == walletId));

        if (transaction is null)
        {
            var message = userRole == "Admin"
                ? $"There is no transaction with ID {id} in the database."
                : $"There is no transaction with ID {id} for this user in the database.";

            _logger.LogWarning("Transaction not found. {Message}", message);
            throw new KeyNotFoundException(message);
        }

        if (userRole == "Admin")
        {
            _logger.LogInformation("Admin accessed transaction {transactionId}", transaction.TransactionId);
            return new TransactionAdminDto
            {
                TransactionId = transaction.TransactionId,
                WalletId = transaction.WalletId,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                RelatedOrderId = transaction.RelatedOrderId,
                CreatedAt = transaction.CreatedAt
            };
        }

        _logger.LogInformation("User accessed their transaction {transactionId}", transaction.TransactionId);
        return new TransactionUserDto
        {
            TransactionId = transaction.TransactionId,
            Amount = transaction.Amount,
            TransactionType = transaction.TransactionType,
            RelatedOrderId = transaction.RelatedOrderId,
            CreatedAt = transaction.CreatedAt
        };
    }
}
