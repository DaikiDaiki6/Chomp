using System;
using OrderService.DTO;

namespace OrderService.Services.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync(int pageNumber, int pageSize);
    Task<ProductDto> GetProductByIdAsync(Guid productId);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task<ProductDto> EditProductAsync(Guid id, EditProductDto dto);
    Task DeleteProductAsync(Guid id); // Changed from Task<bool> to Task
}
