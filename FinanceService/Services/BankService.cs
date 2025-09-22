using System;
using Contracts;
using FinanceService.Data;
using FinanceService.Models;
using FinanceService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceService.Services;

public class BankService : IBankService
{
    private readonly ILogger<BankService> _logger;
    private readonly FinanceDbContext _dbContext;

    public BankService(ILogger<BankService> logger,
        FinanceDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task BankDebit(OrderConfirmedEvent message)
    {

        var newPayment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            Amount = message.TotalPrice,
            PaymentStatus = PaymentStatus.Pending,
            PaymentType = PaymentType.Bank,
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
