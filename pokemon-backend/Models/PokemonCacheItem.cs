using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pokemon_backend.Models
{
    public class PokemonCacheItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PokemonId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string DetailsJson { get; set; } = string.Empty;

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
