using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManagement.Data;
using PropertyManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManagement.Controllers
{
 
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        // Dependencies injected via constructor
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserController> _logger;

        // Constructor with dependency injection
        public UserController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Helper method to validate user type
        private bool IsValidUserType(ApplicationUser user, string[] validUserTypes)
        {
            return validUserTypes.Contains(user.UserType);
        }

        // GET: Returns all users (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ApplicationUser>>> GetAllUsers()
        {
            try
            {
                // Fetch users with their relationships
                var users = await _context.ApplicationUsers
                    .Include(u => u.Properties)
                    .Include(u => u.Interactions)
                    .Include(u => u.Appointments)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching users" });
            }
        }

        // GET: Returns user by ID
        [Authorize(Roles = "Admin,Owner,Client,Broker")]
        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicationUser>> GetUserById(string id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                // Check access permissions
                if (currentUser.Id != id && !isAdmin)
                    return Forbid("You can only view your own data.");

                // Fetch user with relationships
                var user = await _context.ApplicationUsers
                    .Include(u => u.Properties)
                    .Include(u => u.Interactions)
                    .Include(u => u.Appointments)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { message = "User not found." });

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching user with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching user" });
            }
        }

        // POST: Creates new user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApplicationUser>> CreateUser([FromBody] ApplicationUser user)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                user.Id = Guid.NewGuid().ToString();
                await _userManager.CreateAsync(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating user" });
            }
        }

        // PUT: Updates existing user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] ApplicationUser user)
        {
            try
            {
                if (id != user.Id)
                    return BadRequest(new { message = "Incompatible ID." });

                var existingUser = await _userManager.FindByIdAsync(id);
                if (existingUser == null)
                    return NotFound(new { message = "User not found." });

                _context.Entry(existingUser).CurrentValues.SetValues(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.ApplicationUsers.Any(u => u.Id == id))
                    return NotFound(new { message = "User no longer exists." });
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating user" });
            }
        }

        // DELETE: Removes existing user (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                await _userManager.DeleteAsync(user);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting user" });
            }
        }
    }
}
