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
    public class TransactionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TransactionController(AppDbContext context)
        {
            _context = context;
        }

        // Customer topup saldo
        [HttpPost("topup")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Topup([FromBody] TopupDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest(new { message = "Jumlah topup harus lebih dari 0" });

            var username = User.Identity?.Name;
            var customer = await _context.Users
                .SingleOrDefaultAsync(u => u.Username == username);

            if (customer == null) return Unauthorized();

            // Tambahkan saldo
            customer.Saldo += dto.Amount;

            // Simpan transaksi topup
            _context.Transactions.Add(new Transaction
            {
                UserId = customer.Id,
                Amount = dto.Amount,
                Type = "Topup",
                Description = $"Topup saldo sebesar customer {customer.Username} sebesar {dto.Amount}",
                TransactionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Topup berhasil", balance = customer.Saldo });
        }

        // Customer cek saldo
        [HttpGet("my-saldo")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetMySaldo()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

            if (user == null) return Unauthorized();

            return Ok(new { balance = user.Saldo });
        }

        // Customer & Admin cek transaksi pribadi
        [HttpGet("my-transactions")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetMyTransactions()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var transactions = await _context.Transactions
               .Include(t => t.User)
               .Where(t => t.UserId == userId)
               .OrderByDescending(t => t.TransactionDate)
               .Select(t => new TransactionDto
               {
                   Id = t.Id,
                   UserId = t.User.Username,
                   Amount = t.Amount,
                   Type = t.Type,
                   Description = t.Description,
                   TransactionDate = t.TransactionDate
               })
                .ToListAsync();


            return Ok(transactions);
        }

        // Admin cek semua transaksi
        [HttpGet("all-transactions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllTransactions()
        {
            var transactions = await _context.Transactions
                .Include(t => t.User)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    UserId = t.User != null ? t.User.Username : "Unknown",
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync();

            return Ok(transactions);
        }
    }
}
