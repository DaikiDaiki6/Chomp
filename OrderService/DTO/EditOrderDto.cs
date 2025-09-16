using System.ComponentModel.DataAnnotations;
using Contracts;

namespace OrderService.DTO;

public record EditOrderDto(
    PaymentType? PaymentType,
    List<EditOrderItemDto>? OrderItems
); 

