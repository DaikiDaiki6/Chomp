using System;
using FinanceService.Models;

namespace FinanceService.DTO;

public class TransactionDto
{
    public class TransactionAdminDto
    {
        public Guid TransactionId { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public Guid? RelatedOrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TransactionUserDto
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public Guid? RelatedOrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}