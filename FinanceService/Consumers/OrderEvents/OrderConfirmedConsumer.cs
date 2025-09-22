using System;
using Contracts;
using FinanceService.Services;
using FinanceService.Services.Interfaces;
using MassTransit;

namespace FinanceService.OrderEvents.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedConsumer> _logger;
    private readonly ICodService _codService;
    private readonly IBankService _bankService;
    private readonly IEWalletService _eWalletService;
    private readonly ChompWalletService _chompWalletService;

    public OrderConfirmedConsumer(ILogger<OrderConfirmedConsumer> logger,
        ICodService codService,
        IBankService bankService,
        IEWalletService eWalletService,
        ChompWalletService chompWalletService)
    {
        _logger = logger;
        _bankService = bankService;
        _chompWalletService = chompWalletService;
        _eWalletService = eWalletService;
        _codService = codService;
    }
    public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var message = context.Message;
        try
        {
            if (message is null)
            {
                throw new NullReferenceException("OrderConfimedEvent sent no context to the OrderConfirmedConsumer");
            }

            switch (message.PaymentType)
            {
                case PaymentType.ChompWallet:
                    await _chompWalletService.ChompWalletDebit(message);
                    break;
                case PaymentType.EWallet:
                    await _eWalletService.EWalletDebit(message);
                    break;
                case PaymentType.Bank:
                    await _bankService.BankDebit(message);
                    break;
                case PaymentType.COD:
                    await _codService.CodCreation(message);
                    break;
                default:
                    throw new Exception("Invalid Payment Method");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ex}", ex);
        }
    }
}
