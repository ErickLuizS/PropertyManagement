using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PropertyManagement.Controllers;
using PropertyManagement.Data;
using PropertyManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PropertyManagement.Tests
{
    public class InteractionControllerTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<InteractionController>> _mockLogger;
        private readonly InteractionController _controller;

        public InteractionControllerTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<InteractionController>>();

            _controller = new InteractionController(_mockContext.Object, _mockUserManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetInteractions_ReturnsOkResult_WithListOfInteractions()
        {
            // Arrange
            var interactions = new List<Interaction>
            {
                new Interaction { Id = 1, InteractionType = "View", InteractionValue = 1, PropertyId = 101 }
            };
            _mockContext.Setup(c => c.Interactions.Include(It.IsAny<string>()).ToList()).Returns(interactions);

            // Act
            var result = await _controller.GetInteractions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Interaction>>(okResult.Value);
            Assert.Single(returnValue);
        }

        [Fact]
        public async Task GetInteractionsByProperty_ReturnsOkResult_WhenInteractionsExist()
        {
            // Arrange
            var interactions = new List<Interaction>
            {
                new Interaction { Id = 1, InteractionType = "View", InteractionValue = 1, PropertyId = 101 }
            };
            _mockContext.Setup(c => c.Interactions
                .Where(i => i.PropertyId == 101)
                .Include(It.IsAny<string>()).ToList())
                .Returns(interactions);

            // Act
            var result = await _controller.GetInteractionsByProperty(101);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Interaction>>(okResult.Value);
            Assert.Single(returnValue);
        }

        [Fact]
        public async Task GetInteractionsByProperty_ReturnsNotFound_WhenNoInteractionsExist()
        {
            // Arrange
            _mockContext.Setup(c => c.Interactions
                .Where(i => i.PropertyId == 101)
                .Include(It.IsAny<string>()).ToList())
                .Returns(new List<Interaction>());

            // Act
            var result = await _controller.GetInteractionsByProperty(101);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateInteraction_ReturnsCreatedAtAction_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var property = new Property { Id = 101 };
            _mockContext.Setup(c => c.Properties.FindAsync(101)).ReturnsAsync(property);

            var interaction = new Interaction { PropertyId = 101, InteractionType = "Click", InteractionValue = 2 };
            _mockContext.Setup(c => c.Interactions.Add(It.IsAny<Interaction>()));
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.CreateInteraction(interaction);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdInteraction = Assert.IsType<Interaction>(createdResult.Value);
            Assert.Equal(interaction.PropertyId, createdInteraction.PropertyId);
        }

        [Fact]
        public async Task CreateInteraction_ReturnsBadRequest_WhenInvalidData()
        {
            // Arrange
            var interaction = new Interaction { PropertyId = 101, InteractionType = "", InteractionValue = -1 };

            // Act
            var result = await _controller.CreateInteraction(interaction);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid interaction data.", badRequestResult.Value.GetType().GetProperty("message").GetValue(badRequestResult.Value));
        }

        [Fact]
        public async Task UpdateInteraction_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var existingInteraction = new Interaction { Id = 1, CustomerId = "user1", InteractionType = "Click", InteractionValue = 2 };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Interactions.FindAsync(1)).ReturnsAsync(existingInteraction);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var updatedInteraction = new Interaction { Id = 1, InteractionType = "View", InteractionValue = 1 };

            // Act
            var result = await _controller.UpdateInteraction(1, updatedInteraction);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateInteraction_ReturnsForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user2" };
            var existingInteraction = new Interaction { Id = 1, CustomerId = "user1", InteractionType = "Click", InteractionValue = 2 };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Interactions.FindAsync(1)).ReturnsAsync(existingInteraction);

            var updatedInteraction = new Interaction { Id = 1, InteractionType = "View", InteractionValue = 1 };

            // Act
            var result = await _controller.UpdateInteraction(1, updatedInteraction);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        }

        [Fact]
        public async Task DeleteInteraction_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var interaction = new Interaction { Id = 1 };
            _mockContext.Setup(c => c.Interactions.FindAsync(1)).ReturnsAsync(interaction);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteInteraction(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteInteraction_ReturnsNotFound_WhenInteractionDoesNotExist()
        {
            // Arrange
            _mockContext.Setup(c => c.Interactions.FindAsync(1)).ReturnsAsync((Interaction)null);

            // Act
            var result = await _controller.DeleteInteraction(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
