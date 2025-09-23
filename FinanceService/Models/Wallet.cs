namespace FinanceService.Models;

public class Wallet // For ChompWallet only
{
    public Guid WalletId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
