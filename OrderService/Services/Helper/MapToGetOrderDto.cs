using System;
using OrderService.DTO;
using OrderService.Models;

namespace OrderService.Services.Helper;

public class MapToGetOrderDto
{
    public static GetOrderDto GetOrderDtoOutput(Order order)
    {
        var orderItems = order.OrderItems.Select(oi => new GetOrderItemDto(
            oi.OrderItemId,
            oi.Quantity,
            oi.UnitPrice,
            new ProductDto(
                oi.Product.ProductId,
                oi.Product.ProductName,
                oi.Product.Price,
                oi.Product.Stock
            ),
            oi.UnitPrice * oi.Quantity // TotalPrice
        )).ToList();

        return new GetOrderDto(
            order.OrderId,
            order.CustomerId,
            order.PaymentType,
            order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity), // TotalPrice
            order.CreatedAt,
            order.UpdatedAt,
            order.Status,
            orderItems
        );
    }
}
