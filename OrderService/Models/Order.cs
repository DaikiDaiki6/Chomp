using System;
using System.Collections.Generic;
using Contracts;

namespace OrderService.Models;

public enum OrderStatus
{
    Pending, // Order is being edited
    Confirmed,  // Order is processing w/ payment
    Completed, // Order is done
    Cancelled // Order rejected
}

public class Order
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalPrice => OrderItems?.Sum(item => item.TotalPrice) ?? 0;
    public PaymentType PaymentType { get; set; } = PaymentType.ChompWallet;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Navigation propery to OrderItem table
    public List<OrderItem> OrderItems { get; set; } = new();
}
