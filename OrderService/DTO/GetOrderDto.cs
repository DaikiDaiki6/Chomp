using Contracts;
using OrderService.Models;

namespace OrderService.DTO;

public record class GetOrderDto(
    Guid OrderId,
    Guid CustomerId,
    PaymentType PaymentType,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    OrderStatus Status,
    // Navigation propery to OrderItem table
    List<GetOrderItemDto> OrderItems
);

