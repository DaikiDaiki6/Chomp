using System.ComponentModel.DataAnnotations;

namespace OrderService.DTO;

public record CreateOrderItemDto
(
    [Required(ErrorMessage = "Product ID is required.")]
    Guid ProductId,
    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 50, ErrorMessage = "Quantity must be between 1 and 50.")]
    int Quantity
);
