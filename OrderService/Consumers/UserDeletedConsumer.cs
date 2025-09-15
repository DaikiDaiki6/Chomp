using System;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Consumers;

public class UserDeletedConsumer : IConsumer<UserDeletedEvent>
{
    private readonly OrdersDbContext _dbContext;
    private readonly ILogger<UserDeletedConsumer> _logger;

    public UserDeletedConsumer(OrdersDbContext dbContext, ILogger<UserDeletedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserDeletedEvent> context)
    {
    var message = context.Message;
    _logger.LogInformation("Processing user deletion for user {UserId}", message.UserId);

    var updatedCount = await _dbContext.Orders
        .Where(o => o.CustomerId == message.UserId)
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(o => o.Status, OrderStatus.Cancelled));

    _logger.LogInformation("Cancelled {OrderCount} orders for deleted user {UserId}", 
        updatedCount, message.UserId);
}
}
