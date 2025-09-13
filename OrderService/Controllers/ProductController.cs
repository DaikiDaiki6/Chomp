using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.DTO;
using OrderService.Models;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        // Whole Product Controller is for Admin only

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // check if logged in
            _logger.LogInformation("Getting all products - Admin Request");
            try
            {
                var allProducts = await _productService.GetAllAsync();
                return Ok(allProducts);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { errorMessage = ex.Message });
            }
            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetProductById(Guid productId)
        {
            // check if logged in
            _logger.LogInformation("Getting product - Admin Request for product {ProductId}", productId);
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
        public async Task<IActionResult> CreateProduct(CreateProductDto dto)
        {
            // check if logged in
            _logger.LogInformation("CreateProduct endpoint called for product: {ProductName}", dto.ProductName);

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

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> EditProduct(Guid id, EditProductDto dto)
        {
            _logger.LogInformation("EditProduct endpoint called for product: {ProductId}", id);

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

            // else Return Unathorized({message: "You are not authorized for this action."})
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            _logger.LogInformation("DeleteProduct endpoint called for product: {ProductId}", id);

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

            // else Return Unathorized({message: "You are not authorized for this action."})
        }
    }
}
