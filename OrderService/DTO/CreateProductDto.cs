using System.ComponentModel.DataAnnotations;

namespace OrderService.DTO;

public record CreateProductDto(
    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Product name must be between 1 and 256 characters.")]
    string ProductName,
    [Required(ErrorMessage = "Price is required.")]
    [Range(1, 10000, ErrorMessage = "Price must be between 1 and 10,000.")]
    decimal Price,
    [Required(ErrorMessage = "Stock is required.")]
    [Range(0, 99999, ErrorMessage = "Stock must be between 0 and 99,999.")]
    int Stock
);

