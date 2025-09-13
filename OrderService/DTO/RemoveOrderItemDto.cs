using System;
using System.ComponentModel.DataAnnotations;

namespace OrderService.DTO;

public record RemoveOrderItemDto
(
    Guid ProductId,
    [Range(1, 50, ErrorMessage = "If provided, quantity must be between 1 and 50.")]
    int Quantity
);
