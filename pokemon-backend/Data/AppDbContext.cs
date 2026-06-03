using Microsoft.EntityFrameworkCore;
using pokemon_backend.Models;

namespace pokemon_backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<DreamTeamMember> DreamTeams { get; set; } = null!;
        public DbSet<PokemonCacheItem> PokemonCache { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
            });

            // Configure DreamTeamMember entity
            modelBuilder.Entity<DreamTeamMember>(entity =>
            {
                entity.HasIndex(t => new { t.UserId, t.SlotIndex }).IsUnique();
                entity.HasIndex(t => new { t.UserId, t.PokemonId }).IsUnique();
            });

            // Configure PokemonCacheItem entity
            modelBuilder.Entity<PokemonCacheItem>(entity =>
            {
                entity.HasIndex(p => p.Name);
            });
        }
    }
}
