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
    public class TopupController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TopupController(AppDbContext context)
        {
            _context = context;
        }

        //customer topup saldo
        [HttpPost("topup")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Topup([FromBody] TopupDto dto)
        {
            if (dto.Amount <= 0)
                return BadRequest(new { message = "Jumlah topup harus lebih dari 0" });

            // Ambil user yang sedang login
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var customer = await _context.Users.FindAsync(int.Parse(userId));
            if (customer == null) return Unauthorized();

            // Update saldo user
            customer.Saldo += dto.Amount;

            // Simpan ke tabel Topup (histori transaksi)
            var topup = new Topup
            {
                UserId = customer.Id,
                Amount = dto.Amount,
                Type = "Credit",
                TopupDate = DateTime.UtcNow
            };

            _context.Topups.Add(topup);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Topup berhasil",
                balance = customer.Saldo
            });
        }

        //customer cek saldo
        [HttpGet("my-saldo")]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetMySaldo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null) return Unauthorized();

            return Ok(new
            {
                balance = user.Saldo
            });
        }

        //Admin lhat semua transaksi topup
        [HttpGet("transactions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _context.Topups
                .Include(t => t.User)
                .OrderByDescending(t => t.TopupDate)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    UserId = t.User != null ? t.User.Username : "Unknown",
                    Amount = t.Amount,
                    Type = t.Type,
                    TransactionDate = t.TopupDate
                })
                .ToListAsync();

            return Ok(transactions);
        }
    }
}
