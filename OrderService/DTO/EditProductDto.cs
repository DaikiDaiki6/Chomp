namespace OrderService.DTO;

public record EditProductDto(
    string? ProductName,
    decimal? Price,
    int? Stock
);
