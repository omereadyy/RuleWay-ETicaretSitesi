using System.ComponentModel.DataAnnotations;
using Deneme.Data;
using Microsoft.EntityFrameworkCore;

namespace Deneme.Models.CustomValidations
{
    public class PublishValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not Product product)
            {
                return new ValidationResult("Geçersiz ürün");
            }

            if (!product.IsPublished)
            {
                return ValidationResult.Success;
            }

            // Ürün yayınlanmak isteniyorsa kategori kontrolü
            if (product.CategoryId <= 0)
            {
                return new ValidationResult("Ürünün yayınlanabilmesi için bir kategorisi olması gerekir");
            }

            var context = validationContext.GetService<ApplicationDbContext>();
            if (context != null)
            {
                var category = context.Categories.Find(product.CategoryId);
                if (category == null)
                {
                    return new ValidationResult("Geçersiz kategori seçimi");
                }

                // Minimum stok kontrolü
                if (product.StockQuantity < category.MinimumStockQuantity)
                {
                    return new ValidationResult(
                        $"Ürün stok miktarı kategori minimum değerinden ({category.MinimumStockQuantity}) az olamaz");
                }
            }

            return ValidationResult.Success;
        }
    }
}