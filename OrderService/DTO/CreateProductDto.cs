namespace OrderService.DTO;

public record CreateProductDto(
    string ProductName,
    decimal Price,
    int Stock
);

