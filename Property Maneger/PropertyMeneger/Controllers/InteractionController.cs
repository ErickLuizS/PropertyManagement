using Microsoft.AspNetCore.Authorization;
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
    public class InteractionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<InteractionController> _logger;

        public InteractionController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<InteractionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/Interaction
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Interaction>>> GetInteractions()
        {
            try
            {
                var interactions = await _context.Interactions
                    .Include(i => i.Customer)
                    .Include(i => i.Property)
                    .ToListAsync();

                return Ok(interactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching interactions");
                return StatusCode(500, new { message = "Error fetching interactions" });
            }
        }

        // GET: api/Interaction/{propertyId}
        [Authorize]
        [HttpGet("{propertyId}")]
        public async Task<ActionResult<IEnumerable<Interaction>>> GetInteractionsByProperty(int propertyId)
        {
            try
            {
                var interactions = await _context.Interactions
                    .Where(i => i.PropertyId == propertyId)
                    .Include(i => i.Customer)
                    .ToListAsync();

                if (!interactions.Any())
                    return NotFound(new { message = "No interactions found for this property." });

                return Ok(interactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching interactions for property ID {propertyId}");
                return StatusCode(500, new { message = "Error fetching interactions" });
            }
        }

        // POST: api/Interaction
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Interaction>> CreateInteraction([FromBody] Interaction interaction)
        {
            if (string.IsNullOrEmpty(interaction.InteractionType) || interaction.InteractionValue < 0)
            {
                return BadRequest(new { message = "Invalid interaction data." });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var property = await _context.Properties.FindAsync(interaction.PropertyId);
                if (property == null)
                    return NotFound(new { message = "Property not found." });

                interaction.CustomerId = currentUser.Id;
                interaction.InteractionDate = DateTime.UtcNow;

                _context.Interactions.Add(interaction);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInteractionsByProperty), new { propertyId = interaction.PropertyId }, interaction);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating interaction");
                return StatusCode(500, new { message = "Database error creating interaction" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error creating interaction");
                return StatusCode(500, new { message = "Unknown error creating interaction" });
            }
        }

        // PUT: api/Interaction/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInteraction(int id, [FromBody] Interaction updatedInteraction)
        {
            if (string.IsNullOrEmpty(updatedInteraction.InteractionType) || updatedInteraction.InteractionValue < 0)
            {
                return BadRequest(new { message = "Invalid interaction data." });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var existingInteraction = await _context.Interactions.FindAsync(id);
                if (existingInteraction == null)
                    return NotFound(new { message = "Interaction not found." });

                // Verifica se a interação pertence ao cliente atual
                if (existingInteraction.CustomerId != currentUser.Id)
                    return Forbid("You do not have permission to update this interaction.");

                existingInteraction.InteractionType = updatedInteraction.InteractionType;
                existingInteraction.InteractionValue = updatedInteraction.InteractionValue;
                existingInteraction.InteractionDate = DateTime.UtcNow; // Atualiza a data para o momento da atualização

                _context.Interactions.Update(existingInteraction);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Interactions.Any(i => i.Id == id))
                    return NotFound(new { message = "Interaction no longer exists." });

                throw;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Error updating interaction with ID {id}");
                return StatusCode(500, new { message = "Database error updating interaction" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unknown error updating interaction with ID {id}");
                return StatusCode(500, new { message = "Unknown error updating interaction" });
            }
        }

        // DELETE: api/Interaction/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInteraction(int id)
        {
            try
            {
                var interaction = await _context.Interactions.FindAsync(id);
                if (interaction == null)
                    return NotFound(new { message = "Interaction not found." });

                _context.Interactions.Remove(interaction);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error deleting interaction with ID {id}");
                return StatusCode(500, new { message = "Database error deleting interaction" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unknown error deleting interaction with ID {id}");
                return StatusCode(500, new { message = "Unknown error deleting interaction" });
            }
        }
    }
}
