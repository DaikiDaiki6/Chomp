using System;
using Contracts;
using FinanceService.Data;
using FinanceService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FinanceService.Consumers.UserEvents;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly FinanceDbContext _dbContext;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(FinanceDbContext dbContext,
        ILogger<UserCreatedConsumer> logger)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;
        var exists = await _dbContext.Wallets
            .AnyAsync(w => w.CustomerId == message.UserId);

        if (exists)
        {
            _logger.LogWarning("Wallet already exists for user {userId}", message.UserId);
            return;
        }
        
        _logger.LogInformation("Creating a wallet for user with ID {userId}", message.UserId);
        var newUserWallet = new Wallet
        {
            WalletId = Guid.NewGuid(),
            CustomerId = message.UserId,
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Wallets.Add(newUserWallet);
        await _dbContext.SaveChangesAsync();
         _logger.LogInformation("Successfully created a wallet for user with ID {userId} - wallet ID {WalletId}", message.UserId, newUserWallet.WalletId);
    }
}
