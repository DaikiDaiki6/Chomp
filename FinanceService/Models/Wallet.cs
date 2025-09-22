namespace FinanceService.Models;

public class Wallet // For ChompWallet only
{ 
    public Guid WalletId;
    public Guid CustomerId;
    public decimal Balance;
    public DateTime CreatedAt = DateTime.UtcNow;
}
