using System.ComponentModel.DataAnnotations;

namespace Deneme.Models.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MinimumStockQuantity { get; set; }
        public int ProductCount { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Kategori adı gereklidir")]
        [StringLength(100, ErrorMessage = "Kategori adı maksimum 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama maksimum 500 karakter olabilir")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stok miktarı 0 veya daha büyük olmalıdır")]
        public int MinimumStockQuantity { get; set; }
    }

    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Kategori adı gereklidir")]
        [StringLength(100, ErrorMessage = "Kategori adı maksimum 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama maksimum 500 karakter olabilir")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stok miktarı 0 veya daha büyük olmalıdır")]
        public int MinimumStockQuantity { get; set; }
    }
}