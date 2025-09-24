using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaProAPI.Data;
using NexaProAPI.DTOs;
using NexaProAPI.Models;

namespace NexaProAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        //ambil semua akun dalan satu product
        [HttpGet("product/{productId}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetAccountByProduct(int productId)
        {
            var accounts = await _context.Accounts
                .Where(a => a.ProductId == productId)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    Thumbnail = a.Thumbnail,
                    Specification = a.Specification,
                    Price = a.Price,
                    Count = a.Count,
                    ProductId = a.ProductId
                }).ToListAsync();

            return Ok(accounts);
        }

        //ambil detail 1 akun
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            var account = await _context.Accounts
                .Where(a => a.Id == id)
                .Select(a => new AccountDto
                {
                    Id = a.Id,
                    Thumbnail = a.Thumbnail,
                    Specification = a.Specification,
                    Price = a.Price,
                    Count = a.Count,
                    ProductId = a.ProductId
                })
                .FirstOrDefaultAsync();

            if (account == null) return NotFound();
            return Ok(account);
        }

        // Tambah akun baru
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound(new { message = "Product tidak ditemukan" });

            var account = new Account
            {
                Thumbnail = dto.Thumbnail,
                Specification = dto.Specification,
                Price = dto.Price,
                Count = dto.Count,
                ProductId = dto.ProductId
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account berhasil dibuat", accountId = account.Id });
        }

        //Update akun
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAccount(int id , [FromBody] UpdateAccountDto dto)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            account.Thumbnail = dto.Thumbnail;
            account.Specification = dto.Specification;
            account.Price = dto.Price;
            account.Count = dto.Count;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Account berhasil diupdate" });
        }

        //hapus akun
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account berhasil dihapus" });
        }

    }
}
