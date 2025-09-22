using System;
using Contracts;

namespace FinanceService.Services.Interfaces;

public interface IBankService
{
    Task BankDebit(OrderConfirmedEvent message);
}
