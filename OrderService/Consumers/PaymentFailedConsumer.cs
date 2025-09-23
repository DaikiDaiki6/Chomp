using System;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

public class PaymentFailedConsumer: IConsumer<PaymentFailedEvent>
{
    private readonly OrdersDbContext _dbContext;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(OrdersDbContext dbContext, ILogger<PaymentFailedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing payment Failed - PaymentId: {PaymentId}, OrderId: {OrderId}",
            message.PaymentId, message.OrderId);

         try
        {
            // Find the order with its items and products
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == message.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found for payment failed - OrderId: {OrderId}, PaymentId: {PaymentId}", 
                    message.OrderId, message.PaymentId);
                return;
            }

            // Update order status to Pending (since payment failed)
            order.Status = OrderStatus.Pending;
            await _dbContext.SaveChangesAsync();

            Console.WriteLine($"Failed payment processed - PaymentId: {message.PaymentId}, OrderId: {message.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment failed - PaymentId: {PaymentId}, OrderId: {OrderId}", 
                message.PaymentId, message.OrderId);
            throw;
        }
    }
}
