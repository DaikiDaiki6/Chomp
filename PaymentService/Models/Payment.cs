using System;

namespace PaymentService.Models;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public class Payment
{
    public Guid PaymentId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
