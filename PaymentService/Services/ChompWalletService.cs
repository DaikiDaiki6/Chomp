using System;
using Contracts;
using PaymentService.Services.Interfaces;

namespace PaymentService.Services;

public class ChompWalletService : ITransactionService
{
    public Task TransactionAsync(OrderConfirmedEvent? message)
    {
        throw new NotImplementedException();
    }
}
