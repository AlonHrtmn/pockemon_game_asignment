using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using pokemon_backend.Data;
using pokemon_backend.Models;
using pokemon_backend.Services;
using Xunit;

namespace pokemon_backend_tests
{
    public class TeamServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IPokemonService> _pokemonServiceMock;
        private readonly TeamService _teamService;

        public TeamServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _pokemonServiceMock = new Mock<IPokemonService>();
            _teamService = new TeamService(_context, _pokemonServiceMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public async Task AddToTeamAsync_SlotIndexOutOfBounds_ThrowsArgumentException(int invalidSlotIndex)
        {
            // Arrange
            int userId = 1;
            int pokemonId = 25;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _teamService.AddToTeamAsync(userId, pokemonId, invalidSlotIndex));
            
            Assert.Equal("Slot index must be between 0 and 4.", exception.Message);
        }

        [Fact]
        public async Task AddToTeamAsync_PokemonServiceReturnsNull_ReturnsNull()
        {
            // Arrange
            int userId = 1;
            int pokemonId = 25;
            int slotIndex = 2;

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(pokemonId))
                .ReturnsAsync((PokemonDetailsDto?)null);

            // Act
            var result = await _teamService.AddToTeamAsync(userId, pokemonId, slotIndex);

            // Assert
            Assert.Null(result);
            Assert.Empty(_context.DreamTeams);
        }

        [Fact]
        public async Task AddToTeamAsync_ValidSlotEmptyTeam_AddsSuccessfully()
        {
            // Arrange
            int userId = 1;
            int pokemonId = 25;
            int slotIndex = 2;
            var details = new PokemonDetailsDto
            {
                Id = pokemonId,
                Name = "Pikachu",
                SpriteUrl = "http://pikachu-sprite",
                Type1 = "Electric",
                Type2 = null
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(pokemonId))
                .ReturnsAsync(details);

            // Act
            var result = await _teamService.AddToTeamAsync(userId, pokemonId, slotIndex);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(pokemonId, result.PokemonId);
            Assert.Equal("Pikachu", result.PokemonName);
            Assert.Equal("http://pikachu-sprite", result.SpriteUrl);
            Assert.Equal("Electric", result.Type1);
            Assert.Null(result.Type2);
            Assert.Equal(slotIndex, result.SlotIndex);

            var dbMember = await _context.DreamTeams.SingleOrDefaultAsync();
            Assert.NotNull(dbMember);
            Assert.Equal(pokemonId, dbMember.PokemonId);
        }

        [Fact]
        public async Task AddToTeamAsync_SlotOccupied_ReplacesExistingPokemonInSlot()
        {
            // Arrange
            int userId = 1;
            int slotIndex = 2;
            
            // Add existing pokemon to slot 2
            var existingMember = new DreamTeamMember
            {
                UserId = userId,
                PokemonId = 4, // Charmander
                PokemonName = "Charmander",
                SpriteUrl = "url1",
                Type1 = "Fire",
                SlotIndex = slotIndex
            };
            _context.DreamTeams.Add(existingMember);
            await _context.SaveChangesAsync();

            // Set up new pokemon details
            int newPokemonId = 25; // Pikachu
            var newDetails = new PokemonDetailsDto
            {
                Id = newPokemonId,
                Name = "Pikachu",
                SpriteUrl = "url2",
                Type1 = "Electric"
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(newPokemonId))
                .ReturnsAsync(newDetails);

            // Act
            var result = await _teamService.AddToTeamAsync(userId, newPokemonId, slotIndex);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newPokemonId, result.PokemonId);

            // Verify the old member was removed and only the new member exists in slot 2
            var team = await _context.DreamTeams.Where(t => t.UserId == userId).ToListAsync();
            Assert.Single(team);
            Assert.Equal(newPokemonId, team[0].PokemonId);
            Assert.Equal(slotIndex, team[0].SlotIndex);
        }

        [Fact]
        public async Task AddToTeamAsync_PokemonDuplicateDifferentSlot_RemovesDuplicateFromPreviousSlotAndAddsToNewSlot()
        {
            // Arrange
            int userId = 1;
            int oldSlotIndex = 1;
            int newSlotIndex = 3;
            int pokemonId = 25; // Pikachu

            // Pikachu is in slot 1
            var existingMember = new DreamTeamMember
            {
                UserId = userId,
                PokemonId = pokemonId,
                PokemonName = "Pikachu",
                SpriteUrl = "url",
                Type1 = "Electric",
                SlotIndex = oldSlotIndex
            };
            _context.DreamTeams.Add(existingMember);
            await _context.SaveChangesAsync();

            var details = new PokemonDetailsDto
            {
                Id = pokemonId,
                Name = "Pikachu",
                SpriteUrl = "url",
                Type1 = "Electric"
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(pokemonId))
                .ReturnsAsync(details);

            // Act
            var result = await _teamService.AddToTeamAsync(userId, pokemonId, newSlotIndex);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newSlotIndex, result.SlotIndex);

            // Verify the database has only one Pikachu, now in newSlotIndex (3)
            var team = await _context.DreamTeams.Where(t => t.UserId == userId).ToListAsync();
            Assert.Single(team);
            Assert.Equal(pokemonId, team[0].PokemonId);
            Assert.Equal(newSlotIndex, team[0].SlotIndex);
        }

        [Fact]
        public async Task AddToTeamAsync_PokemonDuplicateSameSlot_ReturnsExistingWithoutChanges()
        {
            // Arrange
            int userId = 1;
            int slotIndex = 1;
            int pokemonId = 25;

            var existingMember = new DreamTeamMember
            {
                UserId = userId,
                PokemonId = pokemonId,
                PokemonName = "Pikachu",
                SpriteUrl = "url",
                Type1 = "Electric",
                SlotIndex = slotIndex
            };
            _context.DreamTeams.Add(existingMember);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.AddToTeamAsync(userId, pokemonId, slotIndex);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingMember.Id, result.Id);
            Assert.Equal(slotIndex, result.SlotIndex);

            // GetPokemonDetailsAsync shouldn't even be called because slot is same
            _pokemonServiceMock.Verify(s => s.GetPokemonDetailsAsync(It.IsAny<int>()), Times.Never);

            var team = await _context.DreamTeams.Where(t => t.UserId == userId).ToListAsync();
            Assert.Single(team);
            Assert.Equal(pokemonId, team[0].PokemonId);
            Assert.Equal(slotIndex, team[0].SlotIndex);
        }

        [Fact]
        public async Task RemoveFromTeamAsync_ExistingPokemon_RemovesSuccessfully()
        {
            // Arrange
            int userId = 1;
            int pokemonId = 25;

            var member = new DreamTeamMember
            {
                UserId = userId,
                PokemonId = pokemonId,
                PokemonName = "Pikachu",
                SlotIndex = 0
            };
            _context.DreamTeams.Add(member);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.RemoveFromTeamAsync(userId, pokemonId);

            // Assert
            Assert.True(result);
            Assert.Empty(_context.DreamTeams);
        }

        [Fact]
        public async Task RemoveFromTeamAsync_NonExistingPokemon_ReturnsFalse()
        {
            // Act
            var result = await _teamService.RemoveFromTeamAsync(1, 999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RemoveFromSlotAsync_ExistingSlot_RemovesSuccessfully()
        {
            // Arrange
            int userId = 1;
            int slotIndex = 2;

            var member = new DreamTeamMember
            {
                UserId = userId,
                PokemonId = 25,
                PokemonName = "Pikachu",
                SlotIndex = slotIndex
            };
            _context.DreamTeams.Add(member);
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.RemoveFromSlotAsync(userId, slotIndex);

            // Assert
            Assert.True(result);
            Assert.Empty(_context.DreamTeams);
        }

        [Fact]
        public async Task RemoveFromSlotAsync_EmptySlot_ReturnsFalse()
        {
            // Act
            var result = await _teamService.RemoveFromSlotAsync(1, 2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ClearTeamAsync_ExistingTeam_ClearsSuccessfully()
        {
            // Arrange
            int userId = 1;
            _context.DreamTeams.AddRange(
                new DreamTeamMember { UserId = userId, PokemonId = 1, SlotIndex = 0 },
                new DreamTeamMember { UserId = userId, PokemonId = 2, SlotIndex = 1 }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _teamService.ClearTeamAsync(userId);

            // Assert
            Assert.True(result);
            Assert.Empty(_context.DreamTeams);
        }

        [Fact]
        public async Task ClearTeamAsync_EmptyTeam_ReturnsFalse()
        {
            // Act
            var result = await _teamService.ClearTeamAsync(1);

            // Assert
            Assert.False(result);
        }
    }
}
