using System;

namespace FinanceService.DTO;

public class WalletDto
{
    public class WalletAdminDto
    {
        public Guid WalletId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class WalletUserDto
    {
        public Guid WalletId { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
