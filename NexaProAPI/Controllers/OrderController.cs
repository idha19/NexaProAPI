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
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        //Customer buat pesanan
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            // Ambil userId dari JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User tidak valid.");
            int userId = int.Parse(userIdClaim);

            var customer = await _context.Users.FindAsync(userId);
            if (customer == null) return Unauthorized("Customer tidak ditemukan.");

            var user = await _context.Users.FindAsync(userId);

            if (user == null) return Unauthorized("User tidak ditemukan.");

            //Mulai transaksi kalau proses order gagal misal stok kurang, saldo kurang, dll.
            //Maka sldo customer saldo admin, dan stok account tidak boleh berubah.
            using var transaction = await _context.Database.BeginTransactionAsync();


            try
            {

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Items = new List<OrderItem>()
                };

                decimal totalPrice = 0m;

                // Loop setiap item
                foreach (var item in dto.Items)
                {
                    var account = await _context.Accounts
                        .Include(a => a.Product) // agar bisa akses nama Product
                        .FirstOrDefaultAsync(a => a.Id == item.AccountId);

                    if (account == null)
                        return NotFound($"Account dengan id {item.AccountId} tidak ditemukan.");

                    if (account.Count < item.Quantity)
                        return BadRequest($"Stock account {account.Specification} tidak cukup. Tersisa: {account.Count}");

                    // Kurangi stock
                    account.Count -= item.Quantity;

                    // Hitung subtotal
                    decimal subTotal = account.Price * item.Quantity;
                    totalPrice += subTotal;

                    // Tambahkan ke order items
                    order.Items.Add(new OrderItem
                    {
                        AccountId = account.Id,
                        Quantity = item.Quantity,
                        SubPrice = subTotal
                    });
                }

                //pengecekan saldo customer
                if (customer.Saldo < totalPrice)
                {
                    return BadRequest($"Saldo tidak cukup. Saldo anda: {customer.Saldo}, Total order: {totalPrice}");
                }

                //kurangi saldo customer
                customer.Saldo -= totalPrice;

                //Tambahkan saldo ke admin
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                if (admin != null)
                {
                    admin.Saldo += totalPrice;
                }

                // Simpan total
                order.TotalPrice = totalPrice;

                _context.Orders.Add(order);


                //PENTING YANG BAGIAN TRANSACTION
                // Catat transaksi customer (Purchase Order)
                _context.Transactions.Add(new Transaction
                {
                    UserId = customer.Id,
                    Amount = -order.TotalPrice,   // saldo customer berkurang
                    Type = "Purchase Order",
                    TransactionDate = DateTime.UtcNow
                });

                // Catat transaksi admin (Income from Order)
                if (admin != null)
                {
                    _context.Transactions.Add(new Transaction
                    {
                        UserId = admin.Id,
                        Amount = order.TotalPrice,  // saldo admin bertambah
                        Type = "Income from Order",
                        TransactionDate = DateTime.UtcNow
                    });
                }
                //SAMPAI SINI


                await _context.SaveChangesAsync();

                //PENTING: COMMIT TRANSAKSI AGAR PERUBAHAN SIMPAN DATA BENER2 KESIMPEN
                await transaction.CommitAsync();

                // Ambil order yang sudah tersimpan
                var savedOrder = await _context.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                var response = new OrderResponseDto
                {
                    Id = savedOrder.Id,
                    Username = user.Username,
                    OrderDate = savedOrder.OrderDate,
                    TotalPrice = savedOrder.TotalPrice,
                    Items = savedOrder.Items.Select(i => new OrderItemResponseDto
                    {
                        Id = i.Id,
                        AccountId = i.AccountId,
                        Username = i.Order?.User?.Username ?? "Unknown",
                        ProductName = i.Account?.Product?.Name ?? string.Empty,
                        Specification = i.Account?.Specification ?? string.Empty,
                        Quantity = i.Quantity,
                        SubPrice = i.SubPrice
                    }).ToList()
                };



                return Ok(response);
            }
            catch (Exception ex)
            {
                // Rollback transaksi jika ada error
                await transaction.RollbackAsync();
                return StatusCode(500, $"Terjadi kesalahan saat membuat order: {ex.Message}");
            }
        }

            
        //Customer lihat riwayat orderannya
            
        [HttpGet("my-orders")]
        [Authorize(Roles =  "Customer")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

            if (userIdClaim == null) return Unauthorized("User tidak valid.");
            int userId = int.Parse(userIdClaim);
            string username = usernameClaim ?? "unknown";

            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var response = orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                Username = username,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                Items = o.Items.Select(i => new OrderItemResponseDto
                {
                    Id = i.Id,
                    AccountId = i.AccountId,
                    Username = username,
                    ProductName = i.Account?.Product?.Name ?? string.Empty,
                    Specification = i.Account?.Specification ?? string.Empty,
                    Quantity = i.Quantity,
                    SubPrice = i.SubPrice
                }).ToList()
            });

            

            return Ok(response);
        }

        //Admin lihat semua order
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                .Include(o => o.User)
                .ToListAsync();

            var response = orders.Select(o => new OrderResponseDto
            {
                Id = o.Id,
                Username = o.User?.Username ?? "unknown",
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                Items = o.Items.Select(i => new OrderItemResponseDto
                {
                    Id = i.Id,
                    AccountId = i.AccountId,
                    //Username = i.Order?.User?.Username ?? "Unknown",
                    ProductName = i.Account?.Product?.Name ?? string.Empty,
                    Specification = i.Account?.Specification ?? string.Empty,
                    Quantity = i.Quantity,
                    SubPrice = i.SubPrice
                }).ToList()
            });

            return Ok(response);
        }
    }
}
