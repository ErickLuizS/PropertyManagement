// Path: Property_Meneger_Test/UserControllerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PropertyManagement.Controllers;
using PropertyManagement.Data;
using PropertyManagement.Models;
using Xunit;

namespace Property_Meneger_Test
{
    public class UserControllerTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<UserController>> _mockLogger;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<UserController>>();

            _controller = new UserController(_mockContext.Object, _mockUserManager.Object, _mockLogger.Object);
        }

        // Test to validate exception handling in GetAllUsers
        [Fact]
        public async Task GetAllUsers_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            _mockContext.Setup(c => c.ApplicationUsers.ToList()).Throws(new Exception("Database failure"));

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error fetching users", (statusCodeResult.Value as dynamic).message);
        }

        // Test to validate exception handling in GetUserById
        [Fact]
        public async Task GetUserById_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockUserManager.Setup(um => um.IsInRoleAsync(currentUser, "Admin")).ReturnsAsync(true);

            _mockContext.Setup(c => c.ApplicationUsers.FindAsync("user1")).ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.GetUserById("user1");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error fetching user", (statusCodeResult.Value as dynamic).message);
        }

        // Test to validate exception handling in CreateUser
        [Fact]
        public async Task CreateUser_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var newUser = new ApplicationUser { Id = "user3", UserName = "newUser" };
            _mockUserManager.Setup(um => um.CreateAsync(newUser)).ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.CreateUser(newUser);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error creating user", (statusCodeResult.Value as dynamic).message);
        }

        // Test to validate exception handling in UpdateUser
        [Fact]
        public async Task UpdateUser_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = "user1", UserName = "oldUser" };
            var updatedUser = new ApplicationUser { Id = "user1", UserName = "updatedUser" };

            _mockUserManager.Setup(um => um.FindByIdAsync("user1")).ReturnsAsync(existingUser);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.UpdateUser("user1", updatedUser);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error updating user", (statusCodeResult.Value as dynamic).message);
        }

        // Test to validate exception handling in DeleteUser
        [Fact]
        public async Task DeleteUser_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.FindByIdAsync("user1")).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.DeleteAsync(user)).ThrowsAsync(new Exception("Database failure"));

            // Act
            var result = await _controller.DeleteUser("user1");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error deleting user", (statusCodeResult.Value as dynamic).message);
        }

        // Test to validate authorization restriction for Admin-only access in GetAllUsers
        [Fact]
        public async Task GetAllUsers_ReturnsForbidden_WhenUserNotAdmin()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockUserManager.Setup(um => um.IsInRoleAsync(currentUser, "Admin")).ReturnsAsync(false);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var forbiddenResult = Assert.IsType<ForbidResult>(result);
        }

        // Test to validate authorization restriction for Admin-only access in CreateUser
        [Fact]
        public async Task CreateUser_ReturnsForbidden_WhenUserNotAdmin()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockUserManager.Setup(um => um.IsInRoleAsync(currentUser, "Admin")).ReturnsAsync(false);

            var newUser = new ApplicationUser { Id = "user3", UserName = "newUser" };

            // Act
            var result = await _controller.CreateUser(newUser);

            // Assert
            var forbiddenResult = Assert.IsType<ForbidResult>(result);
        }
    }
}
