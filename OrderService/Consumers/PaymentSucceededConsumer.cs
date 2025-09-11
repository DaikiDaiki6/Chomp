using System;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

public class PaymentSucceededConsumer: IConsumer<PaymentSucceededEvent>
{
    private readonly OrdersDbContext _dbContext;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(OrdersDbContext dbContext, ILogger<PaymentSucceededConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing payment success - PaymentId: {PaymentId}, OrderId: {OrderId}", 
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
                _logger.LogWarning("Order not found for payment success - OrderId: {OrderId}, PaymentId: {PaymentId}", 
                    message.OrderId, message.PaymentId);
                return;
            }

            // Update order status to Completed (since payment succeeded)
            order.Status = OrderStatus.Completed;

            // Reduce stock for each product
            var stockUpdates = new List<string>();
            foreach (var orderItem in order.OrderItems)
            {
                var product = orderItem.Product;
                if (product.Stock < orderItem.Quantity)
                {
                    _logger.LogError("Insufficient stock during payment processing - Product {ProductId} ({ProductName}): Available {AvailableStock}, Required {RequiredQuantity}", 
                        product.ProductId, product.ProductName, product.Stock, orderItem.Quantity);
                    throw new InvalidOperationException($"Insufficient stock for product {product.ProductName}. Available: {product.Stock}, Required: {orderItem.Quantity}");
                }

                product.Stock -= orderItem.Quantity;
                stockUpdates.Add($"{product.ProductName}: {orderItem.Quantity} units (New stock: {product.Stock})");
                
                _logger.LogInformation("Stock reduced - Product {ProductId} ({ProductName}): Reduced by {Quantity}, New stock: {NewStock}", 
                    product.ProductId, product.ProductName, orderItem.Quantity, product.Stock);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment processing completed - OrderId: {OrderId}, PaymentId: {PaymentId}, Status: {OrderStatus}, Stock updates: {StockUpdates}", 
                message.OrderId, message.PaymentId, order.Status, string.Join(", ", stockUpdates));

            Console.WriteLine($"Success payment processed - PaymentId: {message.PaymentId}, OrderId: {message.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment success - PaymentId: {PaymentId}, OrderId: {OrderId}", 
                message.PaymentId, message.OrderId);
            throw;
        }
    }
}
