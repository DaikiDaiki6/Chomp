using System.ComponentModel.DataAnnotations;
using Contracts;
using OrderService.Models;

namespace OrderService.DTO;

public record class CreateOrderDto
(
    [Required(ErrorMessage = "Payment type is required")]
    PaymentType PaymentType,
    List<CreateOrderItemDto> OrderItems
);
