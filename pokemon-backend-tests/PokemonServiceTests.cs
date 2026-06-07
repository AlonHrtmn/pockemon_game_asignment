using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using pokemon_backend.Data;
using pokemon_backend.Models;
using pokemon_backend.Services;
using Xunit;

namespace pokemon_backend_tests
{
    public class PokemonServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly Mock<ILogger<PokemonService>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpHandlerMock;

        public PokemonServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<PokemonService>>();
            _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            _memoryCache.Dispose();
        }

        private PokemonService CreateService()
        {
            var httpClient = new HttpClient(_httpHandlerMock.Object);
            return new PokemonService(_context, httpClient, _loggerMock.Object, _memoryCache);
        }

        [Fact]
        public async Task GetAllPokemonsAsync_Returns151Pokemons()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAllPokemonsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(151, result.Count);
            Assert.Equal("Bulbasaur", result[0].Name);
            Assert.Equal("Mew", result[150].Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(152)]
        [InlineData(-5)]
        public async Task GetPokemonDetailsAsync_InvalidId_ReturnsNull(int invalidId)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_MemoryCacheHit_ReturnsCachedItemWithoutHittingDbOrApi()
        {
            // Arrange
            int pokemonId = 25; // Pikachu
            var cachedDto = new PokemonDetailsDto
            {
                Id = pokemonId,
                Name = "PikachuCachedInMemory",
                Type1 = "electric"
            };
            _memoryCache.Set($"pokemon_{pokemonId}", cachedDto);

            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync(pokemonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PikachuCachedInMemory", result.Name);
            // Verify Http handler was never called
            _httpHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_DbCacheHitNotExpired_ReturnsDbCachedItemAndCachesInMemory()
        {
            // Arrange
            int pokemonId = 25;
            var cachedDto = new PokemonDetailsDto
            {
                Id = pokemonId,
                Name = "PikachuCachedInDb",
                Type1 = "electric"
            };

            var cacheItem = new PokemonCacheItem
            {
                PokemonId = pokemonId,
                Name = "pikachu",
                DetailsJson = JsonSerializer.Serialize(cachedDto),
                LastUpdatedAt = DateTime.UtcNow.AddHours(-1) // 1 hour ago (not expired)
            };

            _context.PokemonCache.Add(cacheItem);
            await _context.SaveChangesAsync();

            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync(pokemonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("PikachuCachedInDb", result.Name);

            // Verify was added to Memory Cache
            Assert.True(_memoryCache.TryGetValue($"pokemon_{pokemonId}", out PokemonDetailsDto? memoryCached));
            Assert.Equal("PikachuCachedInDb", memoryCached?.Name);

            // Verify Http handler was never called
            _httpHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_ApiSuccess_SavesToDbAndMemoryCaches()
        {
            // Arrange
            int pokemonId = 25;
            var apiResponseJson = @"
            {
                ""id"": 25,
                ""name"": ""pikachu"",
                ""height"": 4,
                ""weight"": 60,
                ""sprites"": {
                    ""front_default"": ""http://sprite"",
                    ""other"": {
                        ""official-artwork"": {
                            ""front_default"": ""http://artwork""
                        }
                    }
                },
                ""types"": [
                    { ""slot"": 1, ""type"": { ""name"": ""electric"" } }
                ],
                ""stats"": [
                    { ""base_stat"": 35, ""stat"": { ""name"": ""hp"" } },
                    { ""base_stat"": 55, ""stat"": { ""name"": ""attack"" } },
                    { ""base_stat"": 40, ""stat"": { ""name"": ""defense"" } },
                    { ""base_stat"": 50, ""stat"": { ""name"": ""special-attack"" } },
                    { ""base_stat"": 50, ""stat"": { ""name"": ""special-defense"" } },
                    { ""base_stat"": 90, ""stat"": { ""name"": ""speed"" } }
                ],
                ""abilities"": [
                    { ""ability"": { ""name"": ""static"" } }
                ]
            }";

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains($"/pokemon/{pokemonId}")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(apiResponseJson)
                });

            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync(pokemonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pikachu", result.Name);
            Assert.Equal("electric", result.Type1);
            Assert.Equal(35, result.Hp);
            Assert.Equal(90, result.Speed);

            // Verify memory cache updated
            Assert.True(_memoryCache.TryGetValue($"pokemon_{pokemonId}", out PokemonDetailsDto? memCached));
            Assert.Equal("Pikachu", memCached?.Name);

            // Verify database cache updated
            var dbCached = await _context.PokemonCache.FirstOrDefaultAsync(p => p.PokemonId == pokemonId);
            Assert.NotNull(dbCached);
            Assert.Equal("pikachu", dbCached.Name);
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_ApiFailureWithExpiredDbCache_FallsBackToExpiredDbCache()
        {
            // Arrange
            int pokemonId = 25;
            var cachedDto = new PokemonDetailsDto
            {
                Id = pokemonId,
                Name = "StalePikachu",
                Type1 = "electric"
            };

            var cacheItem = new PokemonCacheItem
            {
                PokemonId = pokemonId,
                Name = "pikachu",
                DetailsJson = JsonSerializer.Serialize(cachedDto),
                LastUpdatedAt = DateTime.UtcNow.AddHours(-25) // Expired (>24 hours)
            };

            _context.PokemonCache.Add(cacheItem);
            await _context.SaveChangesAsync();

            // Setup API failure (Internal Server Error)
            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync(pokemonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("StalePikachu", result.Name); // Fallback to expired cache
            Assert.True(_memoryCache.TryGetValue($"pokemon_{pokemonId}", out PokemonDetailsDto? memCached));
            Assert.Equal("StalePikachu", memCached?.Name);
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_ApiFailureNoDbCache_FallsBackToMockGeneration()
        {
            // Arrange
            int pokemonId = 25; // Pikachu

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                });

            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync(pokemonId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pikachu", result.Name); // Mock details generated
            Assert.Equal("electric", result.Type1);

            // Verify memory cache updated with TTL of mock (which is shorter)
            Assert.True(_memoryCache.TryGetValue($"pokemon_{pokemonId}", out PokemonDetailsDto? memCached));
            Assert.Equal("Pikachu", memCached?.Name);
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_ByName_RetrievesCorrectly()
        {
            // Arrange
            int pokemonId = 25; // Pikachu is 25th in names list
            var cachedDto = new PokemonDetailsDto
            {
                Id = pokemonId,
                Name = "Pikachu",
                Type1 = "electric"
            };
            _memoryCache.Set($"pokemon_{pokemonId}", cachedDto);

            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync("PIKACHU  "); // test trimming & case insensitivity

            // Assert
            Assert.NotNull(result);
            Assert.Equal(pokemonId, result.Id);
            Assert.Equal("Pikachu", result.Name);
        }

        [Fact]
        public async Task GetPokemonDetailsAsync_ByNameInvalid_ReturnsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetPokemonDetailsAsync("invalidpokemonname");

            // Assert
            Assert.Null(result);
        }
    }
}
