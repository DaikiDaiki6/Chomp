using System.ComponentModel.DataAnnotations;

namespace OrderService.DTO;

public record EditProductDto(

    [StringLength(256, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 256 characters.")]
    string? ProductName,
    [Range(1, 10000, ErrorMessage = "If provided, price must be between 1 and 10,000.")]
    decimal? Price,
    [Range(0, 99999, ErrorMessage = "If provided, stock must be between 0 and 99,999.")]
    int? Stock
);
