using Deneme.Models;
using Deneme.Models.DTOs;

namespace Deneme.Extensions
{
    public static class CategoryExtensions
    {
        public static CategoryDto ToDto(this Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                MinimumStockQuantity = category.MinimumStockQuantity,
                ProductCount = category.Products?.Count ?? 0
            };
        }

        public static Category ToEntity(this CreateCategoryDto dto)
        {
            return new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                MinimumStockQuantity = dto.MinimumStockQuantity
            };
        }

        public static void UpdateFromDto(this Category category, UpdateCategoryDto dto)
        {
            category.Name = dto.Name;
            category.Description = dto.Description;
            category.MinimumStockQuantity = dto.MinimumStockQuantity;
        }
    }
}