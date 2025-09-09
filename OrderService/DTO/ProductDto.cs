namespace OrderService.DTO;

public record ProductDto(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Stock
);
