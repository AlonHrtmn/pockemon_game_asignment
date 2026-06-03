using System.Threading.Tasks;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(string username, string password);
        Task<string?> LoginAsync(string username, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}
