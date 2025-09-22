using Contracts;

namespace FinanceService.Models;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public class Payment
{
    public Guid PaymentId;
    public Guid OrderId;
    public Guid CustomerId;
    public decimal Amount;
    public PaymentStatus PaymentStatus = PaymentStatus.Pending;
    public PaymentType PaymentType;
    public DateTime CreatedAt = DateTime.UtcNow;
    public DateTime? UpdatedAt;
};
