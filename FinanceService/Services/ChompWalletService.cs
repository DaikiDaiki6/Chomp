using System;
using Contracts;
using FinanceService.Data;
using FinanceService.Models;
using FinanceService.Services.Interfaces;
using MassTransit;
using MassTransit.Transports;

namespace FinanceService.Services;

public class ChompWalletService
{
    private readonly ILogger<ChompWalletService> _logger;
    private readonly FinanceDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public ChompWalletService(ILogger<ChompWalletService> logger,
        FinanceDbContext dbContext,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task ChompWalletDebit(OrderConfirmedEvent message)
    {
        var wallet = await _dbContext.Wallets.FindAsync(message.CustomerId);

        if (wallet is null)
        {
            throw new Exception($"User with ID {message.CustomerId} does not have a wallet");
        }

        var newPayment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            Amount = message.TotalPrice,
            PaymentStatus = PaymentStatus.Pending,
            PaymentType = PaymentType.ChompWallet,
            CreatedAt = DateTime.UtcNow
        };

        if (wallet.Balance < message.TotalPrice)
        {
            _dbContext.Payments.Add(newPayment);
            await _dbContext.SaveChangesAsync();

            await _publishEndpoint.Publish(new PaymentFailedEvent(
                newPayment.PaymentId,
                newPayment.OrderId,
                newPayment.CustomerId,
                "Insufficient balance in the account",
                DateTime.UtcNow
            ));
            _logger.LogWarning("Payment failed for user {UserId}, insufficient balance. Order {OrderId}",
            message.CustomerId, message.OrderId);
            return;
        }

        wallet.Balance -= message.TotalPrice;
        newPayment.PaymentStatus = PaymentStatus.Completed;
        newPayment.UpdatedAt = DateTime.UtcNow;

        _dbContext.Payments.Add(newPayment);
        await _dbContext.SaveChangesAsync();

        await _publishEndpoint.Publish(new PaymentSucceededEvent(
            newPayment.PaymentId,
                newPayment.OrderId,
                newPayment.CustomerId,
                newPayment.Amount,
                DateTime.UtcNow
        ));

        _logger.LogInformation("Payment succeeded for user {UserId}, order {OrderId}, new balance {Balance}",
        message.CustomerId, message.OrderId, wallet.Balance);
    }
}
