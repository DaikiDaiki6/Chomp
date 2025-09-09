namespace OrderService.DTO;

public record EditOrderDto(
    List<EditOrderItemDto>? OrderItems
); 

