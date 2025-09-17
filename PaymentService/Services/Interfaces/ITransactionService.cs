using System;
using Contracts;

namespace PaymentService.Services.Interfaces;

public interface ITransactionService
{
    Task TransactionAsync(OrderConfirmedEvent? message);
}
