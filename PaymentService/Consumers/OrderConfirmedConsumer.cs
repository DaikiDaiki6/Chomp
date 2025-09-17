using System;
using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;
using PaymentService.Services;
using PaymentService.Services.Interfaces;

namespace PaymentService.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderConfirmedConsumer> _logger;
    private readonly BankService _bankService;
    private readonly ChompWalletService _chompWalletService;
    private readonly CODService _codService;
    private readonly EWalletService _eWalletService;

    public OrderConfirmedConsumer(IPublishEndpoint publishEndpoint,
        ILogger<OrderConfirmedConsumer> logger,
        BankService bankService,
        ChompWalletService chompWalletService,
        CODService codService,
        EWalletService eWalletService)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _bankService = bankService;
        _chompWalletService = chompWalletService;
        _codService = codService;
        _eWalletService = eWalletService;
    }
    public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing payment for Order {OrderId} with PaymentType {PaymentType}", message.OrderId, message.PaymentType);

        if (message.PaymentType == PaymentType.ChompWallet)
        {
            try
            {
                await _chompWalletService.TransactionAsync(message);
                _logger.LogInformation("ChompWallet transaction processed for Order {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the ChompWallet Transaction for Order {OrderId}", message.OrderId);
                // publish error in payment
            }
        }
        else if (message.PaymentType == PaymentType.COD)
        {
            try
            {
                await _codService.TransactionAsync(message);
                _logger.LogInformation("COD transaction processed for Order {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the COD Transaction for Order {OrderId}", message.OrderId);
                // publish error in payment
            }
        }
        else if (message.PaymentType == PaymentType.EWallet)
        {
            try
            {
                await _eWalletService.TransactionAsync(message);
                _logger.LogInformation("EWallet transaction processed for Order {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the E-Wallet Transaction for Order {OrderId}", message.OrderId);
                // publish error in payment
            }
        }
        else if (message.PaymentType == PaymentType.Bank)
        {
            try
            {
                await _bankService.TransactionAsync(message);
                _logger.LogInformation("Bank transaction processed for Order {OrderId}", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in the Bank Transaction for Order {OrderId}", message.OrderId);
                // publish error in payment
            }
        }
        else
        {
            _logger.LogError("Unknown Transaction: {PaymentType} for Order {OrderId}", message.PaymentType, message.OrderId);
            throw new UnknownEventException($"Unknown Transaction: {message.PaymentType}", message.PaymentType.ToString());
        }
    }
}