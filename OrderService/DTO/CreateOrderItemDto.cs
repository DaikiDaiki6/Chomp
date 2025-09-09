namespace OrderService.DTO;

public record CreateOrderItemDto
(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);
