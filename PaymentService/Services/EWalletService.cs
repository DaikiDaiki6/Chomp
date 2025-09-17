using System;
using Contracts;
using PaymentService.Services.Interfaces;

namespace PaymentService.Services;

public class EWalletService : ITransactionService
{
    public Task TransactionAsync(OrderConfirmedEvent? message)
    {
        throw new NotImplementedException();
    }
}
