using System;
using Contracts;

namespace FinanceService.Services.Interfaces;

public interface IEWalletService
{
    Task EWalletDebit(OrderConfirmedEvent message);
}
