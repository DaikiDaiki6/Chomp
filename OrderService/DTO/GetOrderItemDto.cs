using OrderService.Models;

namespace OrderService.DTO;

public record class GetOrderItemDto
(
    Guid OrderItemId,
    int Quantity, 
    decimal UnitPrice,
    ProductDto Product,
    decimal TotalPrice
);
