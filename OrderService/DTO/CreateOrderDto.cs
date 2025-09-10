using System.ComponentModel.DataAnnotations;

namespace OrderService.DTO;

public record class CreateOrderDto
(
    List<CreateOrderItemDto> OrderItems
);
