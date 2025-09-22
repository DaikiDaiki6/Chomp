using System;
using Contracts;

namespace FinanceService.Services.Interfaces;

public interface ICodService
{
    Task CodCreation(OrderConfirmedEvent message);
}
