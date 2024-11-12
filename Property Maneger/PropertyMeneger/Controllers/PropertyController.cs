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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<PropertyController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private bool PropertyExists(int id) => _context.Properties.Any(p => p.Id == id);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Property>>> GetAllProperties()
        {
            try
            {
                var properties = await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Location)
                    .Include(p => p.Images)
                    .Include(p => p.Appointments)
                    .Include(p => p.Interactions)
                    .ToListAsync();

                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching properties");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching properties" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Property>> GetPropertyById(int id)
        {
            try
            {
                var property = await _context.Properties
                    .Include(p => p.Owner)
                    .Include(p => p.Location)
                    .Include(p => p.Images)
                    .Include(p => p.Appointments)
                    .Include(p => p.Interactions)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null)
                    return NotFound(new { message = "Property not found." });

                return Ok(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching property with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error fetching property" });
            }
        }

        [Authorize(Roles = "Owner,Broker")]
        [HttpPost]
        public async Task<ActionResult<Property>> CreateProperty([FromBody] Property property)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                property.OwnerId = currentUser.Id;
                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPropertyById), new { id = property.Id }, property);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating property");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error creating property" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unknown error creating property");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unknown error creating property" });
            }
        }

        [Authorize(Roles = "Owner,Broker")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProperty(int id, [FromBody] Property property)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                if (id != property.Id)
                    return BadRequest(new { message = "ID mismatch." });

                var existingProperty = await _context.Properties.FindAsync(id);
                if (existingProperty == null)
                    return NotFound(new { message = "Property not found." });

                if (existingProperty.OwnerId != currentUser.Id)
                    return Forbid("You do not have permission to modify this property.");

                _context.Entry(existingProperty).CurrentValues.SetValues(property);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyExists(id))
                    return NotFound(new { message = "Property no longer exists." });

                throw;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Error updating property with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating property" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unknown error updating property with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unknown error updating property" });
            }
        }

        [Authorize(Roles = "Owner,Broker")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                    return NotFound(new { message = "Property not found." });

                if (property.OwnerId != currentUser.Id)
                    return Forbid("You do not have permission to delete this property.");

                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error deleting property with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error deleting property" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unknown error deleting property with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unknown error deleting property" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Property>>> SearchProperties([FromQuery] string term)
        {
            try
            {
                var properties = await _context.Properties
                    .Where(p => p.Title.Contains(term) || p.Description.Contains(term) || p.Location.City.Contains(term))
                    .Include(p => p.Images)
                    .ToListAsync();

                if (properties == null || properties.Count == 0)
                    return NotFound(new { message = "No properties found matching the search term." });

                return Ok(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching properties with term: {term}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error searching properties" });
            }
        }

        [Authorize(Roles = "Owner,Broker")]
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadPropertyImage(int id, [FromForm] IFormFile imageFile)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                    return NotFound(new { message = "Property not found." });

                if (property.OwnerId != currentUser.Id)
                    return Forbid("You do not have permission to upload an image for this property.");

                if (imageFile == null || imageFile.Length == 0)
                    return BadRequest(new { message = "Invalid image file." });

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var filePath = Path.Combine("wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var propertyImage = new PropertyImage
                {
                    PropertyId = property.Id,
                    ImageUrl = $"/images/{fileName}"
                };

                property.Images.Add(propertyImage);
                await _context.SaveChangesAsync();

                return Ok(propertyImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image for property with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error uploading image" });
            }
        }

        [Authorize(Roles = "Owner,Broker")]
        [HttpPut("{id}/update-image/{imageId}")]
        public async Task<IActionResult> UpdatePropertyImage(int id, int imageId, [FromForm] IFormFile newImageFile)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var property = await _context.Properties.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
                if (property == null)
                    return NotFound(new { message = "Property not found." });

                if (property.OwnerId != currentUser.Id)
                    return Forbid("You do not have permission to modify this property.");

                var existingImage = property.Images.FirstOrDefault(img => img.Id == imageId);
                if (existingImage == null)
                    return NotFound(new { message = "Image not found." });

                if (newImageFile == null || newImageFile.Length == 0)
                    return BadRequest(new { message = "Invalid image file." });

                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(newImageFile.FileName)}";
                var newFilePath = Path.Combine("wwwroot/images", newFileName);

                using (var stream = new FileStream(newFilePath, FileMode.Create))
                {
                    await newImageFile.CopyToAsync(stream);
                }

                existingImage.ImageUrl = $"/images/{newFileName}";
                await _context.SaveChangesAsync();

                return Ok(existingImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating image for property with ID {id} and Image ID {imageId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error updating image" });
            }
        }
    }
}
