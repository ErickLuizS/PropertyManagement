
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
    public class ContractController : ControllerBase
    {
        // Injected dependencies
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ContractController> _logger;

        // Constructor with dependency injection
        public ContractController(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<ContractController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Returns all contracts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contract>>> GetAllContracts()
        {
            try
            {
                // Fetch contracts including property, customer, and owner details
                var contracts = await _context.Contracts
                    .Include(c => c.Property)
                    .Include(c => c.Customer)
                    .Include(c => c.Owner)
                    .ToListAsync();

                return Ok(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contracts");
                return StatusCode(500, new { message = "Error fetching contracts" });
            }
        }

        // GET: Returns contract by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Contract>> GetContractById(int id)
        {
            try
            {
                // Fetch specific contract with its relationships
                var contract = await _context.Contracts
                    .Include(c => c.Property)
                    .Include(c => c.Customer)
                    .Include(c => c.Owner)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (contract == null)
                    return NotFound(new { message = "Contract not found." });

                return Ok(contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching contract with ID {id}");
                return StatusCode(500, new { message = "Error fetching contract" });
            }
        }

        // POST: Creates new contract
        [Authorize(Roles = "Owner,Broker")]
        [HttpPost]
        public async Task<ActionResult<Contract>> CreateContract([FromBody] Contract contract)
        {
            try
            {
                // Verify user authentication
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Link contract to current owner/broker
                contract.OwnerId = currentUser.Id;
                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContractById), new { id = contract.Id }, contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                return StatusCode(500, new { message = "Error creating contract" });
            }
        }

        // PUT: Updates existing contract
        [Authorize(Roles = "Owner,Broker")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContract(int id, [FromBody] Contract contract)
        {
            try
            {
                // Verify authentication and permissions
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                if (id != contract.Id)
                    return BadRequest(new { message = "Incompatible ID." });

                var existingContract = await _context.Contracts.FindAsync(id);
                if (existingContract == null)
                    return NotFound(new { message = "Contract not found." });

                // Ensure only owner can modify contract
                if (existingContract.OwnerId != currentUser.Id)
                    return Forbid("You don't have permission to modify this contract.");

                _context.Entry(existingContract).CurrentValues.SetValues(contract);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating contract with ID {id}");
                return StatusCode(500, new { message = "Error updating contract" });
            }
        }

        // DELETE: Removes existing contract
        [Authorize(Roles = "Owner,Broker")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContract(int id)
        {
            try
            {
                // Verify authentication and permissions
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return Unauthorized(new { message = "User not authenticated." });

                var contract = await _context.Contracts.FindAsync(id);
                if (contract == null)
                    return NotFound(new { message = "Contract not found." });

                // Ensure only owner can delete contract
                if (contract.OwnerId != currentUser.Id)
                    return Forbid("You don't have permission to delete this contract.");

                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting contract with ID {id}");
                return StatusCode(500, new { message = "Error deleting contract" });
            }
        }

        // Helper method to verify contract existence
        private bool ContractExists(int id) => _context.Contracts.Any(c => c.Id == id);
    }
}
