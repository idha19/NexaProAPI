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

        // 1. Customer Add to Cart
        [HttpPost("add-to-cart")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<OrderResponseDto>> AddToCart([FromBody] CreateOrderDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                .Include(O => O.User)
                .Include(o => o.Items).ThenInclude(i => i.Credentials)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (order == null)
            {
                order = new Order
                {
                    UserId = userId,
                    Status = "Cart",
                    OrderDate = DateTime.UtcNow,
                    Items = new List<OrderItem>()
                };
                _context.Orders.Add(order);
            }

            foreach (var item in dto.Items)
            {
                var account = await _context.Accounts.FindAsync(item.AccountId);
                if (account == null) return NotFound($"Account {item.AccountId} tidak ditemukan.");

                var existing = order.Items.FirstOrDefault(i => i.AccountId == item.AccountId);
                if (existing != null)
                {
                    existing.Quantity += item.Quantity;
                    existing.SubPrice = existing.Quantity * account.Price;
                }
                else
                {
                    order.Items.Add(new OrderItem
                    {
                        AccountId = account.Id,
                        Quantity = item.Quantity,
                        SubPrice = account.Price * item.Quantity
                    });
                }
            }

            order.TotalPrice = order.Items.Sum(i => i.SubPrice);
            await _context.SaveChangesAsync();

            return Ok(ToDto(order));
        }

        // 2. Customer lihat cart
        [HttpGet("my-cart")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<OrderResponseDto>> GetMyCart()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Credentials)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == "Cart");

            if (order == null) return Ok(null);

            return Ok(ToDto(order));
        }

        // 3. Checkout
        [HttpPut("checkout/{orderId}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Checkout(int orderId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId && o.Status == "Cart");

            if (order == null) return BadRequest("Keranjang tidak ditemukan.");

            order.Status = "Pending";
            order.OrderDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(ToDto(order));
        }

        // 4. Admin approve order + simpan credentials
        [HttpPut("approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveOrder([FromBody] ApproveOrderDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Credentials)
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Account)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null || order.Status != "Pending")
                return BadRequest("Order tidak valid.");

            var customer = order.User!;
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");

            if (customer.Saldo < order.TotalPrice)
                return BadRequest("Saldo customer tidak cukup.");

            // saldo customer berkurang
            customer.Saldo -= order.TotalPrice;

            // stok berkurang
            foreach (var item in order.Items)
            {
                var account = await _context.Accounts.FindAsync(item.AccountId);
                if (account == null) continue;
                if (account.Count < item.Quantity)
                    return BadRequest($"Stok {account.Specification} tidak cukup.");
                account.Count -= item.Quantity;
            }

            // saldo admin bertambah
            if (admin != null) admin.Saldo += order.TotalPrice;

            // simpan transaksi
            _context.Transactions.Add(new Transaction
            {
                UserId = customer.Id,
                Amount = order.TotalPrice,
                Type = "Saldo Keluar",
                Description = $"Saldo customer {customer.Username} berkurang {order.TotalPrice} untuk order #{order.Id}",
                TransactionDate = DateTime.UtcNow
            });

            if (admin != null)
            {
                _context.Transactions.Add(new Transaction
                {
                    UserId = admin.Id,
                    Amount = order.TotalPrice,
                    Type = "Saldo Masuk",
                    Description = $"Saldo admin bertambah {order.TotalPrice} dari customer {customer.Username}",
                    TransactionDate = DateTime.UtcNow
                });
            }

            // simpan credentials per order item
            foreach (var delivery in dto.Deliveries)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == delivery.OrderItemId);
                if (item != null)
                {
                    foreach (var cred in delivery.Credentials)
                    {
                        var newCred = new DeliveryCredential
                        {
                            OrderItemId = item.Id,
                            Email = cred.Email,
                            Password = cred.Password
                        };
                        _context.DeliveryCredentials.Add(newCred);
                    }
                }
            }

            order.Status = "Completed";
            order.OrderDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ToDto(order));
        }

        // 5. Admin lihat semua order
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                .Include(o => o.Items).ThenInclude(i => i.Credentials)
                .Include(o => o.User)
                .ToListAsync();

            return Ok(orders.Select(o => ToDto(o)));
        }

        // 6. Customer lihat semua pesanan (riwayat)
        [HttpGet("my-orders")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Account).ThenInclude(a => a.Product)
                .Include(o => o.Items).ThenInclude(i => i.Credentials)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return Ok(orders.Select(o => ToDto(o)));
        }

        // 🔹 Helper converter ke DTO
        private OrderResponseDto ToDto(Order o)
        {
            return new OrderResponseDto
            {
                Id = o.Id,
                Username = o.User?.Username ?? "unknown",
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                Items = o.Items.Select(i => new OrderItemResponseDto
                {
                    Id = i.Id,
                    AccountId = i.AccountId,
                    Username = i.Order?.User?.Username ?? "Unknown",
                    ProductName = i.Account?.Product?.Name ?? string.Empty,
                    Specification = i.Account?.Specification ?? string.Empty,
                    Quantity = i.Quantity,
                    SubPrice = i.SubPrice,
                    Credentials = i.Credentials.Select(c => new CredentialDto
                    {
                        Email = c.Email,
                        Password = c.Password
                    }).ToList()
                }).ToList()
            };
        }
    }
}
