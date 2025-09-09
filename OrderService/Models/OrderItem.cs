using System;

namespace OrderService.Models;

public class OrderItem
{
    public Guid OrderItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Price at time of order

    // Foreign Key to Order
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Navigation Property to Product
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Calculated property
    public decimal TotalPrice => Quantity * UnitPrice;
}
