using System;
using System.Collections.Generic;

namespace OrderService.Models;

public class Order
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation propery to OrderItem table
    public List<OrderItem> OrderItems { get; set; } = new();
}
