using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Deneme.Data;
using Deneme.Models;

namespace Deneme.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchKeyword, int? minStock, int? maxStock)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                ViewData["SearchKeyword"] = searchKeyword;
                var keyword = searchKeyword.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(keyword) ||
                    (p.Description != null && p.Description.ToLower().Contains(keyword)) ||
                    p.Category.Name.ToLower().Contains(keyword));
            }

            if (minStock.HasValue)
            {
                ViewData["MinStock"] = minStock.Value;
                query = query.Where(p => p.StockQuantity >= minStock.Value);
            }

            if (maxStock.HasValue)
            {
                ViewData["MaxStock"] = maxStock.Value;
                query = query.Where(p => p.StockQuantity <= maxStock.Value);
            }

            var products = await query.OrderBy(p => p.Title).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        public async Task<IActionResult> Create()
        {
            try
            {
                var categories = await _context.Categories.ToListAsync();
                _logger.LogInformation("Create GET - Kategoriler yüklendi: {CategoryCount}", categories.Count);
                
                if (!categories.Any())
                {
                    _logger.LogWarning("UYARI: Hiç kategori bulunamadı! Database migration yapıldı mı?");
                    TempData["Error"] = "Hiç kategori bulunamadı! Lütfen önce kategori ekleyin.";
                }
                else
                {
                    foreach (var cat in categories)
                    {
                        _logger.LogInformation("Kategori: {CategoryId} - {CategoryName}", cat.Id, cat.Name);
                    }
                }
                
                ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create GET metodunda hata oluştu");
                TempData["Error"] = "Sayfa yüklenirken hata oluştu: " + ex.Message;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Create([Bind("Title,Description,CategoryId,StockQuantity,IsPublished")] Product product)
        {
            _logger.LogInformation("Create POST başladı - Ürün: {Title}, Kategori: {CategoryId}, Stok: {Stock}, Yayın: {Published}", 
                product.Title ?? "null", product.CategoryId, product.StockQuantity, product.IsPublished);
            
            // Kategori sayısı kontrolü
            var categoryCount = await _context.Categories.CountAsync();
            _logger.LogInformation("Veritabanındaki kategori sayısı: {CategoryCount}", categoryCount);
            
            // Model state hatalarını kontrol et
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Validation Error - {Field}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
            }
            
            // Kategori kontrolü
            var category = await _context.Categories.FindAsync(product.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Geçersiz kategori seçimi");
            }
            else if (product.StockQuantity < category.MinimumStockQuantity)
            {
                ModelState.AddModelError("StockQuantity", $"Stok miktarı minimum {category.MinimumStockQuantity} olmalıdır");
            }
            else if (product.IsPublished && product.StockQuantity < category.MinimumStockQuantity)
            {
                ModelState.AddModelError("IsPublished", $"Ürün yayınlanabilmesi için stok miktarı minimum {category.MinimumStockQuantity} olmalıdır");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.CreatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Ürün veritabanına ekleniyor: {Title}", product.Title);
                    
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Ürün başarıyla kaydedildi: {Title} - ID: {Id}", product.Title, product.Id);
                    TempData["Success"] = "Ürün başarıyla oluşturuldu!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ürün kaydedilirken hata oluştu: {Title}", product.Title);
                    ModelState.AddModelError("", "Ürün kaydedilirken hata oluştu: " + ex.Message);
                }
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,CategoryId,StockQuantity,IsPublished,CreatedAt")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Kategori kontrolü
            var category = await _context.Categories.FindAsync(product.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("CategoryId", "Geçersiz kategori seçimi");
            }
            else if (product.StockQuantity < category.MinimumStockQuantity)
            {
                ModelState.AddModelError("StockQuantity", $"Stok miktarı minimum {category.MinimumStockQuantity} olmalıdır");
            }
            else if (product.IsPublished && product.StockQuantity < category.MinimumStockQuantity)
            {
                ModelState.AddModelError("IsPublished", $"Ürün yayınlanabilmesi için stok miktarı minimum {category.MinimumStockQuantity} olmalıdır");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.UpdatedAt = DateTime.UtcNow;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Ürün başarıyla güncellendi!";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ürün başarıyla silindi!";
            }

            return RedirectToAction("Index");
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}