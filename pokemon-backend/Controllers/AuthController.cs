using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pokemon_backend.Services;

namespace pokemon_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public class CredentialsDto
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] CredentialsDto credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest(new { Message = "Username and password cannot be empty." });
            }

            if (credentials.Username.Length < 3 || credentials.Password.Length < 4)
            {
                return BadRequest(new { Message = "Username must be at least 3 characters, and password at least 4 characters." });
            }

            try
            {
                var user = await _authService.RegisterAsync(credentials.Username, credentials.Password);
                if (user == null)
                {
                    return Conflict(new { message = "Username already exists" });
                }

                return Ok(new { Message = "Registration successful! You can now log in.", Username = user.Username });
            }
            catch (DbUpdateException)
            {
                return StatusCode(503, new { message = "Service temporarily unavailable. Please try again later." });
            }
            catch (Npgsql.NpgsqlException)
            {
                return StatusCode(503, new { message = "Service temporarily unavailable. Please try again later." });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] CredentialsDto credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
            {
                return BadRequest(new { Message = "Username and password cannot be empty." });
            }

            try
            {
                var result = await _authService.LoginAsync(credentials.Username, credentials.Password);

                if (result.ErrorCode == "USER_NOT_FOUND")
                {
                    return Unauthorized(new { message = "User not found" });
                }

                if (result.ErrorCode == "WRONG_PASSWORD")
                {
                    return Unauthorized(new { message = "Wrong password" });
                }

                return Ok(new { Token = result.Token, Username = credentials.Username });
            }
            catch (DbUpdateException)
            {
                return StatusCode(503, new { message = "Service temporarily unavailable. Please try again later." });
            }
            catch (Npgsql.NpgsqlException)
            {
                return StatusCode(503, new { message = "Service temporarily unavailable. Please try again later." });
            }
        }
    }
}
