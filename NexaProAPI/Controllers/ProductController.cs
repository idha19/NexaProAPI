using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaProAPI.Data;
using NexaProAPI.DTOs;
using NexaProAPI.Models;
using System.Security.Claims;

namespace NexaProAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET ALL (Admin & Customer)
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
        {
            var products = await _context.Products.ToListAsync();

            var result = products.Select(p => new ProductDto
            {
                Name = p.Name,
                Logo = p.Logo,
                Category = p.Category.ToString() // enum → string
            });

            return Ok(result);
        }

        // GET BY ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produk tidak ditemukan.");

            var result = new ProductDto
            {
                Name = product.Name,
                Logo = product.Logo,
                Category = product.Category.ToString() // enum → string
            };

            return Ok(result);
        }

        // CREATE PRODUCT (Hanya Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductDto>> CreateProduct(ProductDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User tidak valid.");

            // Convert string → enum
            if (!Enum.TryParse<ProductCategory>(dto.Category, true, out var category))
            {
                return BadRequest("Kategori tidak valid. Hanya 'Game' atau 'Entertainment'.");
            }

            var product = new Product
            {
                Name = dto.Name,
                Logo = dto.Logo,
                Category = category
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // response tetap string biar konsisten dengan DTO
            var result = new ProductDto
            {
                Name = product.Name,
                Logo = product.Logo,
                Category = product.Category.ToString()
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, result);
        }

        // UPDATE PRODUCT (Hanya Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produk tidak ditemukan.");

            // Convert string → enum
            if (!Enum.TryParse<ProductCategory>(dto.Category, true, out var category))
            {
                return BadRequest("Kategori tidak valid. Hanya 'Game' atau 'Entertainment'.");
            }

            product.Name = dto.Name;
            product.Logo = dto.Logo;
            product.Category = category;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Produk berhasil diupdate.",
                product = new ProductDto
                {
                    Name = product.Name,
                    Logo = product.Logo,
                    Category = product.Category.ToString()
                }
            });
        }

        // DELETE PRODUCT (Hanya Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produk tidak ditemukan.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Produk {product.Name} berhasil dihapus." });
        }
    }
}
