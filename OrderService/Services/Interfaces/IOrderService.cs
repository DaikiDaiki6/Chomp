using System;
using OrderService.DTO;

namespace OrderService.Services.Interfaces;

public interface IOrderService
{
    Task<List<GetOrderDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<GetOrderDto> GetOrderByIdAsync(Guid id, Guid userId, string userRole);
    Task<List<GetOrderDto>> GetOrdersByUserIdAsync(Guid userId, int pageNumber, int pageSize);
    Task<GetOrderDto> CreateOrderAsync(CreateOrderDto dto, Guid userId, string userRole);
    Task<GetOrderDto> ConfirmOrderAsync(Guid id, Guid userId, string userRole);
    Task<GetOrderDto> EditOrderAsync(Guid id, EditOrderDto dto, Guid userId, string userRole);
    Task<GetOrderDto> AddOrderItemsAsync(Guid id, List<CreateOrderItemDto> orderItems, Guid userId, string userRole);
    Task<GetOrderDto> RemoveOrderItemsAsync(Guid id, List<RemoveOrderItemDto> itemsToRemove, Guid userId, string userRole);
    Task DeleteOrderAsync(Guid id, Guid userId, string userRole);
}
