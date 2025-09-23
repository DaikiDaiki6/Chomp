namespace FinanceService.Models;

public enum TransactionType
{
    Topup,
    Debit,
    Refund
}
public class Transaction
{
    public Guid TransactionId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public Guid? RelatedOrderId { get; set; } // for debits and refunds
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
