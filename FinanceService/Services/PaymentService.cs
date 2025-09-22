using System;
using FinanceService.Data;
using FinanceService.Models;
using Microsoft.EntityFrameworkCore;
using static FinanceService.DTO.PaymentDto;

namespace FinanceService.Services;

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly FinanceDbContext _dbContext;

    public PaymentService(ILogger<PaymentService> logger,
        FinanceDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<List<PaymentAdminDto>> GetAllPayments(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be 1 or greater.", nameof(pageNumber));
        if (pageSize < 1 || pageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        var payments = await _dbContext.Payments
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return payments.Select(p => new PaymentAdminDto
        {
            PaymentId = p.PaymentId,
            OrderId = p.OrderId,
            CustomerId = p.CustomerId,
            Amount = p.Amount,
            PaymentStatus = p.PaymentStatus,
            PaymentType = p.PaymentType,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<List<PaymentUserDto>> GetMyPayments(Guid userId, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentException("Page number must be 1 or greater.", nameof(pageNumber));
        if (pageSize < 1 || pageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        var payments = await _dbContext.Payments
            .Where(p => p.CustomerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return payments.Select(p => new PaymentUserDto
        {
            PaymentId = p.PaymentId,
            OrderId = p.OrderId,
            Amount = p.Amount,
            PaymentStatus = p.PaymentStatus,
            PaymentType = p.PaymentType,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<object> GetPaymentById(Guid id, Guid userId, string userRole)
    {
        // better checker for Admin/User so we don't have to do two queries.
        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
            p.PaymentId == id && (userRole == "Admin" || p.CustomerId == userId));

        if (payment is null)
        {
            var message = userRole == "Admin"
                ? $"There is no payment with ID {id} in the database."
                : $"There is no payment with ID {id} for this user in the database.";
            throw new KeyNotFoundException(message);
        }

        if (userRole == "Admin")
        {
            return new PaymentAdminDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                CustomerId = payment.CustomerId,
                Amount = payment.Amount,
                PaymentStatus = payment.PaymentStatus,
                PaymentType = payment.PaymentType,
                CreatedAt = payment.CreatedAt
            };
        }

        return new PaymentUserDto
        {
            PaymentId = payment.PaymentId,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentStatus = payment.PaymentStatus,
            PaymentType = payment.PaymentType,
            CreatedAt = payment.CreatedAt
        };
    }
}
