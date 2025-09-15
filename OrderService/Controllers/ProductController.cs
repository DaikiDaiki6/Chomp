using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services.Helper;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly IProductService _productService;

        public ProductController(
            ILogger<ProductController> logger,
            IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(int pageNumber, int pageSize)
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated == true;
            var (userId, userRole, _) = isAuthenticated ? GetCurrentUserInfo.GetUserInfo(User!) : (null, "Anonymous", false);

            _logger.LogInformation("GetAll Endpoint - {Role} Request {UserId}", userRole ?? "Anonymous", userId ?? "N/A");
            try
            {
                var allProducts = await _productService.GetAllAsync(pageNumber, pageSize);
                return Ok(allProducts);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet("{productId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(Guid productId)
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated == true;
            var (userId, userRole, _) = isAuthenticated ? GetCurrentUserInfo.GetUserInfo(User!) : (null, "Anonymous", false);

            _logger.LogInformation("GetProductById Endpoint - Product {ProductId} : {Role} Request {UserId}", 
                productId, userRole ?? "Anonymous", userId ?? "N/A");
            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                return Ok(product);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            _logger.LogInformation("CreateProduct Endpoint - {Role} Request {UserId}", userRole, userId);
            try
            {
                var product = await _productService.CreateProductAsync(dto);
                _logger.LogInformation("Product creation completed successfully for: {ProductId}", product.ProductId);

                return CreatedAtAction(nameof(GetProductById), new { productId = product.ProductId }, product);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Product creation failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditProduct(Guid id, EditProductDto dto)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            _logger.LogInformation("EditProduct Endpoint - Product {ProductId} {Role} Request {UserId}", id, userRole, userId);
            try
            {
                var updatedProduct = await _productService.EditProductAsync(id, dto);
                _logger.LogInformation("Product update completed successfully for: {ProductId}", id);

                return Ok(updatedProduct);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Product not found during update: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Product update failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var (userId, userRole, _) = GetCurrentUserInfo.GetUserInfo(User);

            _logger.LogInformation("DeleteProduct Endpoint - Product {ProductId} {Role} Request {UserId}", id, userRole, userId);
            try
            {
                await _productService.DeleteProductAsync(id);
                _logger.LogInformation("Product deletion completed successfully for: {ProductId}", id);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Product not found during deletion: {ErrorMessage}", ex.Message);
                return NotFound(new { errorMessage = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Product deletion failed: {ErrorMessage}", ex.Message);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }
    }
}
