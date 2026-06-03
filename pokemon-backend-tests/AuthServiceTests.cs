using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using pokemon_backend.Data;
using pokemon_backend.Models;
using pokemon_backend.Services;
using Xunit;

namespace pokemon_backend_tests
{
    public class AuthServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            // Construct configuration using ConfigurationBuilder (simpler and less brittle than mocking Configuration)
            var configData = new Dictionary<string, string?>
            {
                { "Jwt:Key", "SuperSecretKeyThatIsLongEnoughToAvoidErrors1234567890!" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" },
                { "Jwt:DurationInMinutes", "180" }
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _authService = new AuthService(_context, _configuration);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task RegisterAsync_NewUser_RegistersSuccessfullyAndHashesPassword()
        {
            // Arrange
            string username = "testuser";
            string password = "SecretPassword123";

            // Act
            var result = await _authService.RegisterAsync(username, password);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);
            Assert.True(result.Id > 0);
            Assert.NotEmpty(result.PasswordHash);
            Assert.Contains(".", result.PasswordHash); // Salt and hash delimiter

            // Verify it was saved in the db
            var dbUser = await _context.Users.FindAsync(result.Id);
            Assert.NotNull(dbUser);
            Assert.Equal(username, dbUser.Username);
            Assert.Equal(result.PasswordHash, dbUser.PasswordHash);
        }

        [Fact]
        public async Task RegisterAsync_ExistingUserCollision_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            string password = "SecretPassword123";

            // Add existing user to database
            var existingUser = new User
            {
                Username = username,
                PasswordHash = "somehash"
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            // Act - attempt to register again with same username (case-insensitive check)
            var result = await _authService.RegisterAsync("TESTUSER", "AnotherPassword");

            // Assert
            Assert.Null(result);

            // Verify database still only has 1 user
            var userCount = await _context.Users.CountAsync();
            Assert.Equal(1, userCount);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsJwtToken()
        {
            // Arrange
            string username = "testuser";
            string password = "SecretPassword123";

            // Register the user first using the service to have correct password hashing
            await _authService.RegisterAsync(username, password);

            // Act
            var token = await _authService.LoginAsync(username, password);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task LoginAsync_NonExistingUser_ReturnsNull()
        {
            // Act
            var token = await _authService.LoginAsync("nonexistent", "somepassword");

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsNull()
        {
            // Arrange
            string username = "testuser";
            string password = "SecretPassword123";

            await _authService.RegisterAsync(username, password);

            // Act
            var token = await _authService.LoginAsync(username, "wrongpassword");

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void HashAndVerifyPassword_ValidPassword_ReturnsTrue()
        {
            // Arrange
            string password = "SuperSecretPassword";

            // Act
            string hash = _authService.HashPassword(password);
            bool verified = _authService.VerifyPassword(password, hash);

            // Assert
            Assert.True(verified);
        }

        [Fact]
        public void VerifyPassword_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            string password = "SuperSecretPassword";
            string hash = _authService.HashPassword(password);

            // Act
            bool verified = _authService.VerifyPassword("WrongPassword", hash);

            // Assert
            Assert.False(verified);
        }
    }
}
