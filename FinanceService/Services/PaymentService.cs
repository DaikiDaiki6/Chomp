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
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Invalid pagination parameters: pageNumber={PageNumber}, pageSize={PageSize}", pageNumber, pageSize);
            throw new ArgumentException("Invalid pagination parameters.");
        }

        _logger.LogInformation("Fetching all payments: page {PageNumber}, size {PageSize}", pageNumber, pageSize);

        var payments = await _dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} payments for admin", payments.Count);

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
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Invalid pagination for user {UserId}: pageNumber={PageNumber}, pageSize={PageSize}", userId, pageNumber, pageSize);
            throw new ArgumentException("Invalid pagination parameters.");
        }

        _logger.LogInformation("Fetching payments for user {UserId}: page {PageNumber}, size {PageSize}", userId, pageNumber, pageSize);

        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} payments for user {UserId}", payments.Count, userId);

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
        _logger.LogInformation("Fetching payment {PaymentId} for {Role} (UserId={UserId})", id, userRole, userId);

        var payment = await _dbContext.Payments.FirstOrDefaultAsync(p =>
            p.PaymentId == id && (userRole == "Admin" || p.CustomerId == userId));

        if (payment is null)
        {
            var message = userRole == "Admin"
                ? $"Payment with ID {id} not found."
                : $"Payment with ID {id} not found for user {userId}.";

            _logger.LogWarning(message);
            throw new KeyNotFoundException(message);
        }

        _logger.LogInformation("Payment {PaymentId} found for {Role}", id, userRole);

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
