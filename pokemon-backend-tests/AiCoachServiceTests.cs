using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using pokemon_backend.Models;
using pokemon_backend.Services;
using Xunit;

namespace pokemon_backend_tests
{
    public class AiCoachServiceTests
    {
        private readonly Mock<IPokemonService> _pokemonServiceMock;
        private readonly AiCoachService _aiCoachService;

        public AiCoachServiceTests()
        {
            _pokemonServiceMock = new Mock<IPokemonService>();
            _aiCoachService = new AiCoachService(_pokemonServiceMock.Object);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_NullTeam_ReturnsEmptyTeamAdvice()
        {
            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(null!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Empty Team", result.TeamStyle);
            Assert.Contains("empty", result.OverallSummary, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("starter Pokemons", result.CoachAdvice);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_EmptyTeam_ReturnsEmptyTeamAdvice()
        {
            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(new List<DreamTeamMember>());

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Empty Team", result.TeamStyle);
            Assert.Contains("empty", result.OverallSummary, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_EmptyDetailsList_ReturnsGracefulError()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 999, SlotIndex = 0 }
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(999))
                .ReturnsAsync((PokemonDetailsDto?)null);

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.TeamStyle);
            Assert.Contains("Failed to retrieve details", result.OverallSummary);
            Assert.Contains("valid IDs", result.CoachAdvice);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_SpeedBlitzersStyle_ReturnsCorrectStyleAndSummary()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 }
            };

            var details = new PokemonDetailsDto
            {
                Id = 1,
                Name = "FastPoke",
                Type1 = "Electric",
                Speed = 90 // avgSpeed > 85
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(details);

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Equal("Speed Blitzers", result.TeamStyle);
            Assert.Contains("blazingly fast", result.OverallSummary);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_IronFortressStyle_ReturnsCorrectStyleAndSummary()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 }
            };

            var details = new PokemonDetailsDto
            {
                Id = 1,
                Name = "BulkyPoke",
                Type1 = "Normal",
                Hp = 90, // avgHp > 80
                Speed = 50
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(details);

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Equal("Iron Fortress", result.TeamStyle);
            Assert.Contains("immovable wall", result.OverallSummary);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_HyperOffenseStyle_ReturnsCorrectStyleAndSummary()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 }
            };

            var details = new PokemonDetailsDto
            {
                Id = 1,
                Name = "StrongPoke",
                Type1 = "Normal",
                Attack = 90, // avgAttack > 80
                Speed = 50
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(details);

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Equal("Hyper Offense", result.TeamStyle);
            Assert.Contains("firepower", result.OverallSummary);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_BalancedVanguardStyle_ReturnsCorrectStyleAndSummary()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 }
            };

            var details = new PokemonDetailsDto
            {
                Id = 1,
                Name = "BalancedPoke",
                Type1 = "Normal",
                Hp = 70,
                Attack = 70,
                Defense = 70,
                SpecialAttack = 70,
                SpecialDefense = 70,
                Speed = 70
            };

            _pokemonServiceMock
                .Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(details);

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Equal("Balanced Vanguard", result.TeamStyle);
            Assert.Contains("balanced team", result.OverallSummary);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_StarterCoreSynergy_IncludesStarterCoreNote()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 },
                new DreamTeamMember { PokemonId = 2, SlotIndex = 1 },
                new DreamTeamMember { PokemonId = 3, SlotIndex = 2 }
            };

            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(new PokemonDetailsDto { Name = "P1", Type1 = "fire" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(2))
                .ReturnsAsync(new PokemonDetailsDto { Name = "P2", Type1 = "water" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(3))
                .ReturnsAsync(new PokemonDetailsDto { Name = "P3", Type1 = "grass" });

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Contains(result.SynergyNotes, n => n.Contains("Starter Core Unlocked"));
        }

        [Fact]
        public async Task AnalyzeTeamAsync_LetsGoDuoSynergy_IncludesLetsGoDuoNote()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 },
                new DreamTeamMember { PokemonId = 2, SlotIndex = 1 }
            };

            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Pikachu", Type1 = "electric" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(2))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Eevee", Type1 = "normal" });

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Contains(result.SynergyNotes, n => n.Contains("Let's Go Duo"));
        }

        [Fact]
        public async Task AnalyzeTeamAsync_HighVarietySynergy_IncludesHighVarietyNote()
        {
            // Arrange
            var team = new List<DreamTeamMember>();
            for (int i = 1; i <= 6; i++)
            {
                team.Add(new DreamTeamMember { PokemonId = i, SlotIndex = i - 1 });
                _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(i))
                    .ReturnsAsync(new PokemonDetailsDto { Name = $"Poke{i}", Type1 = $"Type{i}" });
            }

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Contains(result.SynergyNotes, n => n.Contains("High Variety"));
        }

        [Fact]
        public async Task AnalyzeTeamAsync_IncompleteSquadSynergy_IncludesIncompleteNote()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 }
            };

            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Pikachu", Type1 = "electric" });

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Contains(result.SynergyNotes, n => n.Contains("Incomplete Squad"));
            Assert.Contains("Complete your team", result.CoachAdvice);
        }

        [Fact]
        public async Task AnalyzeTeamAsync_SharedVulnerabilities_IncludesCorrectSharedVulnerability()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 },
                new DreamTeamMember { PokemonId = 2, SlotIndex = 1 },
                new DreamTeamMember { PokemonId = 3, SlotIndex = 2 },
                new DreamTeamMember { PokemonId = 4, SlotIndex = 3 },
                new DreamTeamMember { PokemonId = 5, SlotIndex = 4 }
            };

            // Fire type is weak to Water, Ground, Rock.
            // 2 fire type Pokemon will trigger a shared vulnerability.
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Char", Type1 = "fire" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(2))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Growl", Type1 = "fire" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(3))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Normal1", Type1 = "normal" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(4))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Normal2", Type1 = "normal" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(5))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Normal3", Type1 = "normal" });

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Contains(result.Weaknesses, w => w.Contains("Shared vulnerability to Water"));
            Assert.Contains(result.Weaknesses, w => w.Contains("Shared vulnerability to Ground"));
            Assert.Contains(result.Weaknesses, w => w.Contains("Shared vulnerability to Rock"));
        }

        [Fact]
        public async Task AnalyzeTeamAsync_NewTypesWeaknesses_CalculatesCorrectWeaknesses()
        {
            // Arrange
            var team = new List<DreamTeamMember>
            {
                new DreamTeamMember { PokemonId = 1, SlotIndex = 0 },
                new DreamTeamMember { PokemonId = 2, SlotIndex = 1 },
                new DreamTeamMember { PokemonId = 3, SlotIndex = 2 },
                new DreamTeamMember { PokemonId = 4, SlotIndex = 3 },
                new DreamTeamMember { PokemonId = 5, SlotIndex = 4 }
            };

            // 2 Fighting type Pokemon. Weaknesses: flying, psychic, fairy
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(1))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Machop", Type1 = "fighting" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(2))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Machoke", Type1 = "fighting" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(3))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Normal1", Type1 = "normal" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(4))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Normal2", Type1 = "normal" });
            _pokemonServiceMock.Setup(s => s.GetPokemonDetailsAsync(5))
                .ReturnsAsync(new PokemonDetailsDto { Name = "Normal3", Type1 = "normal" });

            // Act
            var result = await _aiCoachService.AnalyzeTeamAsync(team);

            // Assert
            Assert.Contains(result.Weaknesses, w => w.Contains("Shared vulnerability to Flying"));
            Assert.Contains(result.Weaknesses, w => w.Contains("Shared vulnerability to Psychic"));
            Assert.Contains(result.Weaknesses, w => w.Contains("Shared vulnerability to Fairy"));
        }
    }
}
