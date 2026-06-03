using System.Collections.Generic;
using System.Threading.Tasks;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public interface ITeamService
    {
        Task<List<DreamTeamMember>> GetTeamAsync(int userId);
        Task<DreamTeamMember?> AddToTeamAsync(int userId, int pokemonId, int slotIndex);
        Task<bool> RemoveFromTeamAsync(int userId, int pokemonId);
        Task<bool> RemoveFromSlotAsync(int userId, int slotIndex);
        Task<bool> ClearTeamAsync(int userId);
    }
}
