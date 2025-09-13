using OrderService.Models;

namespace OrderService.DTO;

public record class GetOrderDto(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalPrice,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    OrderStatus Status,
    // Navigation propery to OrderItem table
    List<GetOrderItemDto> OrderItems
);

