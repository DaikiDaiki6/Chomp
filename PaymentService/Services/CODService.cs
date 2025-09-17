using System;
using Contracts;
using MassTransit;
using PaymentService.Services.Interfaces;

namespace PaymentService.Services;

public class CODService : ITransactionService
{
    private readonly IPublishEndpoint _publishEndpoint;
    public CODService(IPublishEndpoint publishEndpoint) {
        _publishEndpoint = publishEndpoint;
    }
    public Task TransactionAsync(OrderConfirmedEvent? message)
    {
        throw new NotImplementedException();
    }
}
