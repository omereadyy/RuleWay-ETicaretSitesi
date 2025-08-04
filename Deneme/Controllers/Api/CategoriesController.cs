using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Deneme.Data;
using Deneme.Models;
using Deneme.Models.DTOs;
using Deneme.Extensions;

namespace Deneme.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .Select(c => c.ToDto())
                .ToListAsync();

            return Ok(ApiResponse<List<CategoryDto>>.SuccessResult(categories, "Kategoriler başarıyla getirildi"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(ApiResponse<CategoryDto>.ErrorResult("Kategori bulunamadı"));
            }

            return Ok(ApiResponse<CategoryDto>.SuccessResult(category.ToDto(), "Kategori başarıyla getirildi"));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(CreateCategoryDto categoryDto)
        {
            _logger.LogInformation("Yeni kategori oluşturma isteği: {CategoryName}", categoryDto.Name);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("Kategori oluşturma doğrulama hatası: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Doğrulama hatası", errors));
            }

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryDto.Name.ToLower());

            if (existingCategory != null)
            {
                _logger.LogWarning("Duplicate kategori adı: {CategoryName}", categoryDto.Name);
                return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Bu isimde bir kategori zaten mevcut"));
            }

            var category = categoryDto.ToEntity();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            var createdCategory = await _context.Categories
                .Include(c => c.Products)
                .FirstAsync(c => c.Id == category.Id);

            _logger.LogInformation("Kategori başarıyla oluşturuldu. ID: {CategoryId}, Ad: {CategoryName}", 
                category.Id, category.Name);

            return CreatedAtAction(nameof(GetCategory), 
                new { id = category.Id }, 
                ApiResponse<CategoryDto>.SuccessResult(createdCategory.ToDto(), "Kategori başarıyla oluşturuldu"));
        }        
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, UpdateCategoryDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Doğrulama hatası", errors));
            }

            var existingCategory = await _context.Categories.FindAsync(id);
            if (existingCategory == null)
            {
                return NotFound(ApiResponse<CategoryDto>.ErrorResult("Kategori bulunamadı"));
            }

            // Aynı isimde başka kategori var mı kontrol et (kendisi hariç)
            var duplicateCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryDto.Name.ToLower() && c.Id != id);

            if (duplicateCategory != null)
            {
                return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Bu isimde bir kategori zaten mevcut"));
            }

            existingCategory.UpdateFromDto(categoryDto);

            try
            {
                await _context.SaveChangesAsync();
                
                var updatedCategory = await _context.Categories
                    .Include(c => c.Products)
                    .FirstAsync(c => c.Id == id);
                    
                return Ok(ApiResponse<CategoryDto>.SuccessResult(updatedCategory.ToDto(), "Kategori başarıyla güncellendi"));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
                {
                    return NotFound(ApiResponse<CategoryDto>.ErrorResult("Kategori bulunamadı"));
                }
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Kategori bulunamadı"));
            }

            // Kategoriye ait ürün var mı kontrol et
            if (category.Products.Any())
            {
                return BadRequest(ApiResponse<object>.ErrorResult(
                    $"Bu kategoriye ait {category.Products.Count} ürün bulunmaktadır. Önce ürünleri başka kategoriye taşıyın veya silin."));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null!, "Kategori başarıyla silindi"));
        }

        [HttpGet("{id}/products")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetCategoryProducts(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(ApiResponse<List<ProductDto>>.ErrorResult("Kategori bulunamadı"));
            }

            var totalCount = await _context.Products
                .Where(p => p.CategoryId == id)
                .CountAsync();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == id)
                .OrderBy(p => p.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.ToDto())
                .ToListAsync();

            var response = new PaginatedResponse<ProductDto>
            {
                Data = products,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PaginatedResponse<ProductDto>>.SuccessResult(response, 
                $"{category.Name} kategorisindeki ürünler başarıyla getirildi"));
        }

        [HttpGet("with-product-counts")]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategoriesWithProductCounts()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    MinimumStockQuantity = c.MinimumStockQuantity,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            return Ok(ApiResponse<List<CategoryDto>>.SuccessResult(categories, 
                "Kategoriler ve ürün sayıları başarıyla getirildi"));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}