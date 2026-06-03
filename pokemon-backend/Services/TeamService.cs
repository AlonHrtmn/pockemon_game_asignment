using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using pokemon_backend.Data;
using pokemon_backend.Models;

namespace pokemon_backend.Services
{
    public class TeamService : ITeamService
    {
        private readonly AppDbContext _context;
        private readonly IPokemonService _pokemonService;

        public TeamService(AppDbContext context, IPokemonService pokemonService)
        {
            _context = context;
            _pokemonService = pokemonService;
        }

        public async Task<List<DreamTeamMember>> GetTeamAsync(int userId)
        {
            return await _context.DreamTeams
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.SlotIndex)
                .ToListAsync();
        }

        public async Task<DreamTeamMember?> AddToTeamAsync(int userId, int pokemonId, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 4)
            {
                throw new ArgumentException("Slot index must be between 0 and 4.");
            }

            // 1. Check if the Pokemon is already on the team in another slot (prevent duplicates)
            var duplicate = await _context.DreamTeams
                .FirstOrDefaultAsync(t => t.UserId == userId && t.PokemonId == pokemonId);
            
            if (duplicate != null)
            {
                if (duplicate.SlotIndex == slotIndex)
                {
                    // Already in this exact slot, do nothing and return it
                    return duplicate;
                }
                
                // Remove the duplicate from its previous slot first so we can move it
                _context.DreamTeams.Remove(duplicate);
            }

            // 2. Fetch Pokemon details
            var details = await _pokemonService.GetPokemonDetailsAsync(pokemonId);
            if (details == null)
            {
                return null;
            }

            // 3. Check if there's already a Pokemon in the target slot and replace it
            var existingInSlot = await _context.DreamTeams
                .FirstOrDefaultAsync(t => t.UserId == userId && t.SlotIndex == slotIndex);
            
            if (existingInSlot != null)
            {
                _context.DreamTeams.Remove(existingInSlot);
            }

            // 4. Create and save new team member
            var member = new DreamTeamMember
            {
                UserId = userId,
                PokemonId = pokemonId,
                PokemonName = details.Name,
                SpriteUrl = details.SpriteUrl,
                Type1 = details.Type1,
                Type2 = details.Type2,
                SlotIndex = slotIndex,
                AddedAt = DateTime.UtcNow
            };

            _context.DreamTeams.Add(member);
            await _context.SaveChangesAsync();

            return member;
        }

        public async Task<bool> RemoveFromTeamAsync(int userId, int pokemonId)
        {
            var member = await _context.DreamTeams
                .FirstOrDefaultAsync(t => t.UserId == userId && t.PokemonId == pokemonId);
            
            if (member == null)
            {
                return false;
            }

            _context.DreamTeams.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromSlotAsync(int userId, int slotIndex)
        {
            var member = await _context.DreamTeams
                .FirstOrDefaultAsync(t => t.UserId == userId && t.SlotIndex == slotIndex);
            
            if (member == null)
            {
                return false;
            }

            _context.DreamTeams.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearTeamAsync(int userId)
        {
            var team = await _context.DreamTeams.Where(t => t.UserId == userId).ToListAsync();
            if (!team.Any())
            {
                return false;
            }

            _context.DreamTeams.RemoveRange(team);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
