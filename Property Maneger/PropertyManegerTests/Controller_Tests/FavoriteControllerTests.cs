using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PropertyManagement.Controllers;
using PropertyManagement.Data;
using PropertyManagement.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Tests
{
    public class FavoriteControllerTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<FavoriteController>> _mockLogger;
        private readonly FavoriteController _controller;

        public FavoriteControllerTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<FavoriteController>>();

            _controller = new FavoriteController(_mockContext.Object, _mockUserManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetFavorites_ReturnsOkResult_WithListOfFavorites()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var favorites = new List<Favorite>
            {
                new Favorite { Id = 1, ClientId = "user1", PropertyId = 101 }
            };
            _mockContext.Setup(c => c.Favorites
                .Include(It.IsAny<string>()).Where(f => f.ClientId == currentUser.Id).ToList())
                .Returns(favorites);

            // Act
            var result = await _controller.GetFavorites();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Favorite>>(okResult.Value);
            Assert.Single(returnValue);
        }

        [Fact]
        public async Task GetFavorites_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.GetFavorites();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
        }

        [Fact]
        public async Task AddFavorite_ReturnsCreatedAtAction_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var property = new Property { Id = 101 };
            _mockContext.Setup(c => c.Properties.FindAsync(101)).ReturnsAsync(property);

            _mockContext.Setup(c => c.Favorites.Add(It.IsAny<Favorite>()));
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.AddFavorite(101);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdFavorite = Assert.IsType<Favorite>(createdResult.Value);
            Assert.Equal(101, createdFavorite.PropertyId);
        }

        [Fact]
        public async Task AddFavorite_ReturnsConflict_WhenAlreadyFavorited()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var existingFavorite = new Favorite { Id = 1, ClientId = "user1", PropertyId = 101 };
            _mockContext.Setup(c => c.Favorites
                .FirstOrDefault(f => f.ClientId == currentUser.Id && f.PropertyId == 101))
                .Returns(existingFavorite);

            // Act
            var result = await _controller.AddFavorite(101);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("Property already in favorites.", conflictResult.Value.GetType().GetProperty("message").GetValue(conflictResult.Value));
        }

        [Fact]
        public async Task AddFavorite_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.AddFavorite(101);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
        }

        [Fact]
        public async Task RemoveFavorite_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var favorite = new Favorite { Id = 1, ClientId = "user1", PropertyId = 101 };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Favorites.FirstOrDefault(f => f.ClientId == currentUser.Id && f.PropertyId == 101)).Returns(favorite);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.RemoveFavorite(101);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task RemoveFavorite_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.RemoveFavorite(101);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
        }

        [Fact]
        public async Task RemoveFavorite_ReturnsNotFound_WhenFavoriteDoesNotExist()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Favorites.FirstOrDefault(f => f.ClientId == currentUser.Id && f.PropertyId == 101)).Returns((Favorite)null);

            // Act
            var result = await _controller.RemoveFavorite(101);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Favorite not found.", notFoundResult.Value.GetType().GetProperty("message").GetValue(notFoundResult.Value));
        }
    }
}
