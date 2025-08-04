using System.ComponentModel.DataAnnotations;

namespace Deneme.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı gereklidir")]
        [StringLength(100, ErrorMessage = "Kategori adı maksimum 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama maksimum 500 karakter olabilir")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stok miktarı 0 veya daha büyük olmalıdır")]
        public int MinimumStockQuantity { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}