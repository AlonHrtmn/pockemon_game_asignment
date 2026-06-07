using System.Threading.Tasks;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public class LoginResultDto
    {
        public string? Token { get; set; }
        public string? ErrorCode { get; set; } // "USER_NOT_FOUND", "WRONG_PASSWORD"
    }

    public interface IAuthService
    {
        Task<User?> RegisterAsync(string username, string password);
        Task<LoginResultDto> LoginAsync(string username, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}
