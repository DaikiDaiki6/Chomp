namespace FinanceService.Models;

public enum TransactionType
{
    Topup,
    Debit,
    Refund
}
public class Transaction
{
    public Guid TransactionId;
    public Guid WalletId;
    public decimal Amount;
    public TransactionType TransactionType;
    public Guid? RelatedOrderId; // for debits and refunds
    public DateTime CreatedAt = DateTime.UtcNow;
}
