using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManagement.Data;
using PropertyManagement.Models;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PropertyManagement.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController : ControllerBase
    {
        // Dependency injection
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentController> _logger;
        private readonly IAmazonSimpleEmailService _emailService;

        // Controller constructor
        public AppointmentController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AppointmentController> logger,
            IAmazonSimpleEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        // Helper method for sending email notifications
        private async Task<bool> TrySendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // Email configuration setup
                var sendRequest = new SendEmailRequest
                {
                    Source = "your-email@domain.com", // Replace with your SES verified email
                    Destination = new Destination { ToAddresses = new List<string> { toEmail } },
                    Message = new Message
                    {
                        Subject = new Content(subject),
                        Body = new Body { Html = new Content(body) }
                    }
                };

                var response = await _emailService.SendEmailAsync(sendRequest);
                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail} with subject {subject}");
                return false;
            }
        }

        // GET: Retrieves all appointments from database
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAllAppointments()
        {
            try
            {
                // Fetch appointments including client and property information
                var appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Property)
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching appointments");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching appointments" });
            }
        }

        // POST: Creates a new appointment
        [Authorize(Roles = "Client")]
        [HttpPost]
        public async Task<ActionResult<Appointment>> CreateAppointment([FromBody] Appointment appointment)
        {
            try
            {
                // Verify current user
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                appointment.ClientId = currentUser.Id;
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Fetch property information
                var property = await _context.Properties.FindAsync(appointment.PropertyId);
                if (property == null)
                    return NotFound(new { message = "Property not found." });

                // Send confirmation email
                string emailBody = $"Your visit to property {property.Title} is scheduled for {appointment.AppointmentDate}.";
                bool emailSent = await TrySendEmailAsync(currentUser.Email, "Appointment Confirmation", emailBody);

                if (!emailSent)
                    _logger.LogWarning($"Failed to send confirmation email to {currentUser.Email}");

                return CreatedAtAction(nameof(GetAllAppointments), new { id = appointment.Id }, appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating appointment" });
            }
        }

        // PUT: Updates an existing appointment
        [Authorize(Roles = "Client")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] Appointment appointment)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var existingAppointment = await _context.Appointments.FindAsync(id);
                if (existingAppointment == null)
                    return NotFound(new { message = "Appointment not found." });

                // Verify permissions and appointment validity
                if (existingAppointment.ClientId != currentUser.Id)
                    return Forbid("You don't have permission to modify this appointment.");

                if (existingAppointment.AppointmentDate < DateTime.Now)
                    return BadRequest(new { message = "Cannot update past appointments." });

                _context.Entry(existingAppointment).CurrentValues.SetValues(appointment);
                await _context.SaveChangesAsync();

                // Send update notification
                var property = await _context.Properties.FindAsync(appointment.PropertyId);
                string emailBody = $"Your appointment for property {property.Title} has been updated to {appointment.AppointmentDate}.";
                await TrySendEmailAsync(currentUser.Email, "Appointment Update", emailBody);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating appointment {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating appointment" });
            }
        }

        // DELETE: Removes an existing appointment
        [Authorize(Roles = "Client")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null)
                    return NotFound(new { message = "Appointment not found." });

                // Verify permissions and appointment validity
                if (appointment.ClientId != currentUser.Id)
                    return Forbid("You don't have permission to delete this appointment.");

                if (appointment.AppointmentDate < DateTime.Now)
                    return BadRequest(new { message = "Cannot delete past appointments." });

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                // Send cancellation notification
                var property = await _context.Properties.FindAsync(appointment.PropertyId);
                string emailBody = $"Your appointment for property {property.Title} scheduled for {appointment.AppointmentDate} has been cancelled.";
                await TrySendEmailAsync(currentUser.Email, "Appointment Cancellation", emailBody);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting appointment {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting appointment" });
            }
        }
    }
}
