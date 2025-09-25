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
                SubPrice = oi.SubPrice,
                //OrderedBy = oi.Order?.User?.Username ?? "Unknown"
            });

            return Ok(response);
        }
    }
}