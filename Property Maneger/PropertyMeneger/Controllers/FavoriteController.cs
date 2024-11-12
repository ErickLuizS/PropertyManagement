using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyManagement.Data;
using PropertyManagement.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PropertyManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FavoriteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<FavoriteController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/Favorite
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Favorite>>> GetFavorites()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var favorites = await _context.Favorites
                    .Include(f => f.Property)
                    .ThenInclude(p => p.Owner)
                    .Where(f => f.ClientId == currentUser.Id)
                    .ToListAsync();

                return Ok(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching favorites");
                return StatusCode(500, new { message = "Error fetching favorites" });
            }
        }

        // POST: api/Favorite
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Favorite>> AddFavorite([FromBody] int propertyId)
        {
            if (propertyId <= 0)
            {
                return BadRequest(new { message = "Invalid property ID." });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var property = await _context.Properties.FindAsync(propertyId);
                if (property == null)
                    return NotFound(new { message = "Property not found." });

                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.ClientId == currentUser.Id && f.PropertyId == propertyId);

                if (existingFavorite != null)
                    return Conflict(new { message = "Property already in favorites." });

                var favorite = new Favorite
                {
                    ClientId = currentUser.Id,
                    PropertyId = propertyId
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFavorites), new { id = favorite.Id }, favorite);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error adding favorite");
                return StatusCode(500, new { message = "Database error adding favorite" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error adding favorite");
                return StatusCode(500, new { message = "Unknown error adding favorite" });
            }
        }

        // DELETE: api/Favorite/{propertyId}
        [Authorize]
        [HttpDelete("{propertyId}")]
        public async Task<IActionResult> RemoveFavorite(int propertyId)
        {
            if (propertyId <= 0)
            {
                return BadRequest(new { message = "Invalid property ID." });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.ClientId == currentUser.Id && f.PropertyId == propertyId);

                if (favorite == null)
                    return NotFound(new { message = "Favorite not found." });

                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error removing favorite for property ID {propertyId}");
                return StatusCode(500, new { message = "Database error removing favorite" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unknown error removing favorite for property ID {propertyId}");
                return StatusCode(500, new { message = "Unknown error removing favorite" });
            }
        }
    }
}
