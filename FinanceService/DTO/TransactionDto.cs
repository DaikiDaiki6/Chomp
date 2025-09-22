using System;
using FinanceService.Models;

namespace FinanceService.DTO;

public class TransactionDto
{
    public class TransactionAdminDto
    {
        public Guid TransactionId;
        public Guid WalletId;
        public decimal Amount;
        public TransactionType TransactionType;
        public Guid? RelatedOrderId;
        public DateTime CreatedAt;
    }

    public class TransactionUserDto
    {
        public Guid TransactionId;
        public decimal Amount;
        public TransactionType TransactionType;
        public Guid? RelatedOrderId;
        public DateTime CreatedAt;
    }
}