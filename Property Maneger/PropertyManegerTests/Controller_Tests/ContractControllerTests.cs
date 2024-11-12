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
    public class ContractControllerTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<ContractController>> _mockLogger;
        private readonly ContractController _controller;

        public ContractControllerTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<ContractController>>();

            _controller = new ContractController(_mockContext.Object, _mockUserManager.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllContracts_ReturnsOkResult_WithListOfContracts()
        {
            // Arrange
            var contracts = new List<Contract> { new Contract { Id = 1, Amount = 500, StartDate = DateTime.Now } };
            _mockContext.Setup(c => c.Contracts.Include(It.IsAny<string>()).ToList()).Returns(contracts);

            // Act
            var result = await _controller.GetAllContracts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<Contract>>(okResult.Value);
            Assert.Single(returnValue);
        }

        [Fact]
        public async Task GetContractById_ReturnsContract_WhenContractExists()
        {
            // Arrange
            var contract = new Contract { Id = 1, Amount = 500, StartDate = DateTime.Now };
            _mockContext.Setup(c => c.Contracts.Include(It.IsAny<string>()).FirstOrDefault(c => c.Id == 1)).Returns(contract);

            // Act
            var result = await _controller.GetContractById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<Contract>(okResult.Value);
            Assert.Equal(contract.Id, returnValue.Id);
        }

        [Fact]
        public async Task GetContractById_ReturnsNotFound_WhenContractDoesNotExist()
        {
            // Arrange
            _mockContext.Setup(c => c.Contracts.Include(It.IsAny<string>()).FirstOrDefault(c => c.Id == 1)).Returns((Contract)null);

            // Act
            var result = await _controller.GetContractById(1);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateContract_ReturnsCreatedAtAction_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var contract = new Contract { Id = 1, Amount = 1000, StartDate = DateTime.Now };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Contracts.Add(It.IsAny<Contract>()));
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.CreateContract(contract);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdContract = Assert.IsType<Contract>(createdResult.Value);
            Assert.Equal(contract.Id, createdContract.Id);
        }

        [Fact]
        public async Task CreateContract_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);
            var contract = new Contract { Id = 1, Amount = 1000 };

            // Act
            var result = await _controller.CreateContract(contract);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
        }

        [Fact]
        public async Task UpdateContract_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var contract = new Contract { Id = 1, Amount = 1500, StartDate = DateTime.Now };
            var existingContract = new Contract { Id = 1, Amount = 1000, OwnerId = "user1" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Contracts.FindAsync(1)).ReturnsAsync(existingContract);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.UpdateContract(1, contract);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateContract_ReturnsForbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user2" };
            var contract = new Contract { Id = 1, Amount = 1500, StartDate = DateTime.Now };
            var existingContract = new Contract { Id = 1, Amount = 1000, OwnerId = "user1" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Contracts.FindAsync(1)).ReturnsAsync(existingContract);

            // Act
            var result = await _controller.UpdateContract(1, contract);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        }

        [Fact]
        public async Task DeleteContract_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var contract = new Contract { Id = 1, OwnerId = "user1" };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Contracts.FindAsync(1)).ReturnsAsync(contract);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteContract(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteContract_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.DeleteContract(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value.GetType().GetProperty("message").GetValue(unauthorizedResult.Value));
        }
    }
}
