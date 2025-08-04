using Deneme.Models;
using Deneme.Models.DTOs;

namespace Deneme.Extensions
{
    public static class ProductExtensions
    {
        public static ProductDto ToDto(this Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                StockQuantity = product.StockQuantity,
                IsPublished = product.IsPublished,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        public static Product ToEntity(this CreateProductDto dto)
        {
            return new Product
            {
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                StockQuantity = dto.StockQuantity,
                IsPublished = dto.IsPublished,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateFromDto(this Product product, UpdateProductDto dto)
        {
            product.Title = dto.Title;
            product.Description = dto.Description;
            product.CategoryId = dto.CategoryId;
            product.StockQuantity = dto.StockQuantity;
            product.IsPublished = dto.IsPublished;
            product.UpdatedAt = DateTime.UtcNow;
        }
    }
}