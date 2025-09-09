namespace OrderService.DTO;

public record class CreateOrderDto
(
    Guid CustomerId,
    List<CreateOrderItemDto> OrderItems
);
