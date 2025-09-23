using Microsoft.AspNetCore.Mvc;
using NexaProAPI.DTOs;
using NexaProAPI.Services;

namespace NexaProAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        //REGISTER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var (user, token) = await _authService.Register(dto);

                return Ok(new
                {
                    message = "Register berhasil",
                    user = new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role,
                        user.Saldo,
                    },
                    token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        //LOGIN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var token = await _authService.Login(dto);

                return Ok(new
                {
                    message = "Login berhasil",
                    token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
