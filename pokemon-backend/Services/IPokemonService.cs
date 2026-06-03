using System.Collections.Generic;
using System.Threading.Tasks;

namespace pokemon_backend.Services
{
    public class PokemonListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SpriteUrl { get; set; } = string.Empty;
        public string Type1 { get; set; } = string.Empty;
        public string? Type2 { get; set; }
    }

    public class PokemonDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SpriteUrl { get; set; } = string.Empty;
        public string Type1 { get; set; } = string.Empty;
        public string? Type2 { get; set; }
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
        public List<string> Abilities { get; set; } = new();
        public int Height { get; set; }
        public int Weight { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public interface IPokemonService
    {
        Task<List<PokemonListItemDto>> GetAllPokemonsAsync();
        Task<PokemonDetailsDto?> GetPokemonDetailsAsync(int id);
        Task<PokemonDetailsDto?> GetPokemonDetailsAsync(string name);
    }
}
