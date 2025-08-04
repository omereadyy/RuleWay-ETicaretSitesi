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
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetProducts(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            var totalCount = await _context.Products.CountAsync();
            
            var products = await _context.Products
                .Include(p => p.Category)
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

            return Ok(ApiResponse<PaginatedResponse<ProductDto>>.SuccessResult(response, "Ürünler başarıyla getirildi"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(ApiResponse<ProductDto>.ErrorResult("Ürün bulunamadı"));
            }

            return Ok(ApiResponse<ProductDto>.SuccessResult(product.ToDto(), "Ürün başarıyla getirildi"));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(CreateProductDto productDto)
        {
            _logger.LogInformation("Yeni ürün oluşturma isteği: {ProductTitle}", productDto.Title);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("Ürün oluşturma doğrulama hatası: {Errors}", string.Join(", ", errors));
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Doğrulama hatası", errors));
            }

            var category = await _context.Categories.FindAsync(productDto.CategoryId);
            if (category == null)
            {
                _logger.LogWarning("Geçersiz kategori ID: {CategoryId}", productDto.CategoryId);
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Geçersiz kategori seçimi"));
            }

            if (productDto.StockQuantity < category.MinimumStockQuantity)
            {
                _logger.LogWarning("Yetersiz stok miktarı. Ürün: {ProductTitle}, Stok: {Stock}, Minimum: {MinStock}", 
                    productDto.Title, productDto.StockQuantity, category.MinimumStockQuantity);
                return BadRequest(ApiResponse<ProductDto>.ErrorResult(
                    $"Stok miktarı minimum {category.MinimumStockQuantity} olmalıdır"));
            }

            var product = productDto.ToEntity();
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var createdProduct = await _context.Products
                .Include(p => p.Category)
                .FirstAsync(p => p.Id == product.Id);

            _logger.LogInformation("Ürün başarıyla oluşturuldu. ID: {ProductId}, Başlık: {ProductTitle}", 
                product.Id, product.Title);

            return CreatedAtAction(nameof(GetProduct), 
                new { id = product.Id }, 
                ApiResponse<ProductDto>.SuccessResult(createdProduct.ToDto(), "Ürün başarıyla oluşturuldu"));
        }

    
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, UpdateProductDto productDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Doğrulama hatası", errors));
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                return NotFound(ApiResponse<ProductDto>.ErrorResult("Ürün bulunamadı"));
            }

            var category = await _context.Categories.FindAsync(productDto.CategoryId);
            if (category == null)
            {
                return BadRequest(ApiResponse<ProductDto>.ErrorResult("Geçersiz kategori seçimi"));
            }

            if (productDto.StockQuantity < category.MinimumStockQuantity)
            {
                return BadRequest(ApiResponse<ProductDto>.ErrorResult(
                    $"Stok miktarı minimum {category.MinimumStockQuantity} olmalıdır"));
            }

            existingProduct.UpdateFromDto(productDto);

            try
            {
                await _context.SaveChangesAsync();
                
                var updatedProduct = await _context.Products
                    .Include(p => p.Category)
                    .FirstAsync(p => p.Id == id);
                    
                return Ok(ApiResponse<ProductDto>.SuccessResult(updatedProduct.ToDto(), "Ürün başarıyla güncellendi"));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound(ApiResponse<ProductDto>.ErrorResult("Ürün bulunamadı"));
                }
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Ürün bulunamadı"));
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(null!, "Ürün başarıyla silindi"));
        }

        [HttpGet("filter")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> FilterProducts(
            [FromQuery] string? searchKeyword,
            [FromQuery] int? minStock,
            [FromQuery] int? maxStock,
            [FromQuery] int? categoryId,
            [FromQuery] bool? isPublished,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // Arama anahtar kelimesi filtresi
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                var keyword = searchKeyword.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(keyword) ||
                    (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                    p.Category.Name.ToLower().Contains(keyword));
            }

            // Stok miktar aralığı filtresi
            if (minStock.HasValue)
            {
                query = query.Where(p => p.StockQuantity >= minStock.Value);
            }

            if (maxStock.HasValue)
            {
                query = query.Where(p => p.StockQuantity <= maxStock.Value);
            }

            // Kategori filtresi
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Yayın durumu filtresi
            if (isPublished.HasValue)
            {
                query = query.Where(p => p.IsPublished == isPublished.Value);
            }

            var totalCount = await query.CountAsync();
            
            var products = await query
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

            return Ok(ApiResponse<PaginatedResponse<ProductDto>>.SuccessResult(response, "Ürünler başarıyla filtrelendi"));
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> CreateBulkProducts(List<CreateProductDto> productDtos)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<List<ProductDto>>.ErrorResult("Doğrulama hatası", validationErrors));
            }

            var createdProducts = new List<ProductDto>();
            var errors = new List<string>();

            foreach (var dto in productDtos)
            {
                var category = await _context.Categories.FindAsync(dto.CategoryId);
                if (category == null)
                {
                    errors.Add($"'{dto.Title}' ürünü için geçersiz kategori seçimi");
                    continue;
                }

                if (dto.StockQuantity < category.MinimumStockQuantity)
                {
                    errors.Add($"'{dto.Title}' ürünü için stok miktarı minimum {category.MinimumStockQuantity} olmalıdır");
                    continue;
                }

                var product = dto.ToEntity();
                _context.Products.Add(product);
            }

            if (errors.Any())
            {
                return BadRequest(ApiResponse<List<ProductDto>>.ErrorResult("Bazı ürünler oluşturulamadı", errors));
            }

            await _context.SaveChangesAsync();

            var allCreatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => productDtos.Select(dto => dto.Title).Contains(p.Title))
                .Select(p => p.ToDto())
                .ToListAsync();

            return Ok(ApiResponse<List<ProductDto>>.SuccessResult(allCreatedProducts, $"{allCreatedProducts.Count} ürün başarıyla oluşturuldu"));
        }

        [HttpPut("bulk-publish")]
        public async Task<ActionResult<ApiResponse<object>>> BulkPublishProducts(List<int> productIds)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (!products.Any())
            {
                return NotFound(ApiResponse<object>.ErrorResult("Belirtilen ürünler bulunamadı"));
            }

            var errors = new List<string>();
            var updatedCount = 0;

            foreach (var product in products)
            {
                if (product.StockQuantity < product.Category.MinimumStockQuantity)
                {
                    errors.Add($"'{product.Title}' ürünü minimum stok miktarını karşılamıyor");
                    continue;
                }

                product.IsPublished = true;
                product.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _context.SaveChangesAsync();

            var message = $"{updatedCount} ürün yayınlandı";
            if (errors.Any())
            {
                message += $", {errors.Count} ürün yayınlanamadı";
            }

            return Ok(ApiResponse<object>.SuccessResult(new { UpdatedCount = updatedCount, Errors = errors }, message));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}