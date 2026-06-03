using System;

namespace pokemon_backend.Models
{
    public class DreamTeamMember
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PokemonId { get; set; }
        public string PokemonName { get; set; } = string.Empty;
        public string SpriteUrl { get; set; } = string.Empty;
        public string Type1 { get; set; } = string.Empty;
        public string? Type2 { get; set; }
        public int SlotIndex { get; set; } // 0 to 4
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
