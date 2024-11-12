// Path: Property_Meneger_Test/AppointmentControllerTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
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
    public class AppointmentControllerTests
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<AppointmentController>> _mockLogger;
        private readonly Mock<IAmazonSimpleEmailService> _mockEmailService;
        private readonly AppointmentController _controller;

        public AppointmentControllerTests()
        {
            _mockContext = new Mock<AppDbContext>(new DbContextOptions<AppDbContext>());
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _mockLogger = new Mock<ILogger<AppointmentController>>();
            _mockEmailService = new Mock<IAmazonSimpleEmailService>();

            _controller = new AppointmentController(_mockContext.Object, _mockUserManager.Object, _mockLogger.Object, _mockEmailService.Object);
        }

        [Fact]
        public async Task GetAllAppointments_ReturnsOk_WhenDataExists()
        {
            // Arrange
            var appointments = new List<Appointment>
    {
        new Appointment { Id = 1, ClientId = "user1" },
        new Appointment { Id = 2, ClientId = "user2" }
    };

            var mockAppointments = new Mock<DbSet<Appointment>>();
            mockAppointments.As<IQueryable<Appointment>>().Setup(m => m.Provider).Returns(appointments.AsQueryable().Provider);
            mockAppointments.As<IQueryable<Appointment>>().Setup(m => m.Expression).Returns(appointments.AsQueryable().Expression);
            mockAppointments.As<IQueryable<Appointment>>().Setup(m => m.ElementType).Returns(appointments.AsQueryable().ElementType);
            mockAppointments.As<IQueryable<Appointment>>().Setup(m => m.GetEnumerator()).Returns(() => appointments.GetEnumerator());

            _mockContext.Setup(c => c.Appointments).Returns(mockAppointments.Object);

            // Act
            var result = await _controller.GetAllAppointments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnAppointments = Assert.IsType<List<Appointment>>(okResult.Value);
            Assert.Equal(2, returnAppointments.Count);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsCreatedAtAction_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            var appointment = new Appointment { Id = 1, ClientId = currentUser.Id, PropertyId = 1, AppointmentDate = DateTime.Now.AddDays(1) };

            _mockContext.Setup(c => c.Properties.FindAsync(1)).ReturnsAsync(new Property { Id = 1, Title = "Test Property" });
            _mockContext.Setup(c => c.Appointments.Add(It.IsAny<Appointment>()));
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<SendEmailRequest>(), default)).ReturnsAsync(new SendEmailResponse { HttpStatusCode = HttpStatusCode.OK });

            // Act
            var result = await _controller.CreateAppointment(appointment);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result);
            var createdAppointment = Assert.IsType<Appointment>(actionResult.Value);
            Assert.Equal(appointment.Id, createdAppointment.Id);
        }

        [Fact]
        public async Task UpdateAppointment_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var existingAppointment = new Appointment { Id = 1, ClientId = currentUser.Id, AppointmentDate = DateTime.Now.AddDays(1) };
            var updatedAppointment = new Appointment { Id = 1, ClientId = currentUser.Id, AppointmentDate = DateTime.Now.AddDays(2) };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Appointments.FindAsync(1)).ReturnsAsync(existingAppointment);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.UpdateAppointment(1, updatedAppointment);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAppointment_ReturnsNoContent_WhenValid()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var appointment = new Appointment { Id = 1, ClientId = currentUser.Id, AppointmentDate = DateTime.Now.AddDays(1) };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Appointments.FindAsync(1)).ReturnsAsync(appointment);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteAppointment(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task CreateAppointment_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);
            var appointment = new Appointment { Id = 1 };

            // Act
            var result = await _controller.CreateAppointment(appointment);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task UpdateAppointment_ReturnsForbidden_WhenNotOwner()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var appointment = new Appointment { Id = 1, ClientId = "user2" }; // Different client

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Appointments.FindAsync(1)).ReturnsAsync(appointment);

            // Act
            var result = await _controller.UpdateAppointment(1, appointment);

            // Assert
            var forbiddenResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        }

        [Fact]
        public async Task DeleteAppointment_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.DeleteAppointment(1);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User not authenticated.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task DeleteAppointment_ReturnsBadRequest_WhenAppointmentPassed()
        {
            // Arrange
            var currentUser = new ApplicationUser { Id = "user1" };
            var appointment = new Appointment { Id = 1, ClientId = currentUser.Id, AppointmentDate = DateTime.Now.AddDays(-1) };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);
            _mockContext.Setup(c => c.Appointments.FindAsync(1)).ReturnsAsync(appointment);

            // Act
            var result = await _controller.DeleteAppointment(1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Cannot delete an appointment that has already passed.", badRequestResult.Value);
        }
    }

    
}
