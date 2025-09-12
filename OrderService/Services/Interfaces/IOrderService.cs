using System;
using OrderService.DTO;

namespace OrderService.Services.Interfaces;

public interface IOrderService
{
    Task<List<GetOrderDto>> GetAllAsync();
    Task<GetOrderDto> GetOrderByIdAsync(Guid id);
    Task<GetOrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<GetOrderDto> ConfirmOrderAsync(Guid id);
    Task<GetOrderDto> EditOrderAsync(Guid id, EditOrderDto dto);
    Task<GetOrderDto> AddOrderItemsAsync(Guid id, List<CreateOrderItemDto> orderItems);
    Task<GetOrderDto> RemoveOrderItemsAsync(Guid id, List<Guid> orderItemIds);
    Task DeleteOrderAsync(Guid id);
}
