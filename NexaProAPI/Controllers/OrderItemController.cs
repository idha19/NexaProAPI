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
    public class OrderItemController : ControllerBase
    {
        private readonly AppDbContext _context;
        public OrderItemController(AppDbContext context)
        {
            _context = context;
        }

        //customer lihat order item miliknya
        [HttpGet("my-items")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<IEnumerable<OrderItemResponseDto>>> GetMyOrderItems()
        {
            //ambil userId dari token JWT
            var UserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (UserIdClaim == null) return Unauthorized("User tidak valid.");
            int userId = int.Parse(UserIdClaim);

            var items = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Account)
                .ThenInclude(a => a.Product)
                .Where(oi => oi.Order.UserId == userId)
                .ToListAsync();

            var response = items.Select(oi => new OrderItemResponseDto
            {
                Id = oi.Id,
                //OrderId = oi.OrderId,
                AccountId = oi.AccountId,
                ProductName = oi.Account?.Product?.Name ?? string.Empty,
                Specification = oi.Account?.Specification ?? string.Empty,
                Quantity = oi.Quantity,
                SubPrice = oi.SubPrice
            });
            return Ok(response);
        }

        //Admin lihat semua order item
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderItemResponseDto>>> GetAllOrderItems()
        {
            var items = await _context.OrderItems
                .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
                .Include(oi => oi.Account)
                .ThenInclude(a => a.Product)
                .ToListAsync();

            var response = items.Select(oi => new OrderItemResponseDto
            {
                Id = oi.Id,
                //OrderId = oi.OrderId,
                Username = oi.Order?.User?.Username ?? "Unknown",
                AccountId = oi.AccountId,
                ProductName = oi.Account?.Product?.Name ?? string.Empty,
                Specification = oi.Account?.Specification ?? string.Empty,
                Quantity = oi.Quantity,
                SubPrice = oi.SubPrice,
                //OrderedBy = oi.Order?.User?.Username ?? "Unknown"
            });

            return Ok(response);
        }

        //Admin update order item

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrderItemResponseDto>> UpdateOrderItem(int id, [FromBody] UpdateOrderItemDto dto)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.Id == id);

            if (orderItem == null)
                return NotFound("Order item tidak ditemukan");

            // update field
            orderItem.Quantity = dto.Quantity;
            orderItem.SubPrice = dto.SubPrice;

            // (opsional) ganti account
            if (dto.AccountId.HasValue)
                orderItem.AccountId = dto.AccountId.Value;

            // update total harga order induknya
            var order = orderItem.Order;
            if (order != null)
            {
                order.TotalPrice = await _context.OrderItems
                    .Where(oi => oi.OrderId == order.Id)
                    //.SumAsync(oi => oi.SubPrice * oi.Quantity);
                    .SumAsync(oi => oi.SubPrice);
        }

            await _context.SaveChangesAsync();

            // mapping response
            var response = new OrderItemResponseDto
            {
                Id = orderItem.Id,
                //OrderId = orderItem.OrderId,
                AccountId = orderItem.AccountId,
                ProductName = orderItem.Account?.Product?.Name ?? string.Empty,
                Specification = orderItem.Account?.Specification ?? string.Empty,
                Quantity = orderItem.Quantity,
                SubPrice = orderItem.SubPrice,
                //OrderedBy = orderItem.Order?.User?.Username ?? "Unknown"
            };

            return Ok(response);
        }
    }
}