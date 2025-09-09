using System;

namespace OrderService.DTO;

public record EditOrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);
