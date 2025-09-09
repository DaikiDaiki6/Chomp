using System;
using System.Collections.Generic;

namespace OrderService.Models;

public enum OrderStatus
{
    Pending,
    Completed,
    Refunded
}
public class Order
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalPrice => OrderItems?.Sum(item => item.TotalPrice) ?? 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Navigation propery to OrderItem table
    public List<OrderItem> OrderItems { get; set; } = new();
}
