using System;
using Contracts;
using FinanceService.Data;
using FinanceService.Models;
using FinanceService.Services.Interfaces;

namespace FinanceService.Services;

public class EWalletService : IEWalletService
{
    private readonly ILogger<EWalletService> _logger;
    private readonly FinanceDbContext _dbContext;

    public EWalletService(ILogger<EWalletService> logger,
        FinanceDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    public async Task EWalletDebit(OrderConfirmedEvent message)
    {
        var newPayment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            Amount = message.TotalPrice,
            PaymentStatus = PaymentStatus.Pending,
            PaymentType = PaymentType.COD,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _dbContext.Payments.Add(newPayment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Created new payment for user {UserId}, order {OrderId}, payment {PaymentId}",
                message.CustomerId, message.OrderId, newPayment.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment for user {UserId}, order {OrderId}",
                message.CustomerId, message.OrderId);
            throw;
        }
    }
}
