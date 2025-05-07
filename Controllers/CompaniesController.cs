using Microsoft.AspNetCore.Mvc;
using CompanyManagementSystem.Models;
using CompanyManagementSystem.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace CompanyManagementSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(AppDbContext context, ILogger<CompaniesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int GetCurrentUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User ID claim not found in token");
                    throw new UnauthorizedAccessException("User ID not found in token");
                }
                return int.Parse(userIdClaim.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID from token");
                throw new UnauthorizedAccessException("Invalid token");
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<Company>> Get()
        {
            try
            {
                var userId = GetCurrentUserId();
                return _context.Companies.Where(c => c.UserId == userId).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies");
                return StatusCode(500, new { message = "An error occurred while retrieving companies" });
            }
        }

        [HttpGet("{id}")]
        public ActionResult<Company> Get(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = _context.Companies.FirstOrDefault(c => c.Id == id && c.UserId == userId);
                if (company == null) return NotFound();
                return company;
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving company {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the company" });
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] Company company)
        {
            try
            {
                company.UserId = GetCurrentUserId();
                _context.Companies.Add(company);
                _context.SaveChanges();
                return Ok(new { id = company.Id, message = "Company created successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, new { message = "An error occurred while creating the company" });
            }
        }

        [HttpPut("{id}")]
        public ActionResult Put(int id, [FromBody] Company company)
        {
            try
            {
                var userId = GetCurrentUserId();
                var existingCompany = _context.Companies.FirstOrDefault(c => c.Id == id && c.UserId == userId);
                if (existingCompany == null) return NotFound();

                existingCompany.Name = company.Name;
                existingCompany.Address = company.Address;
                _context.SaveChanges();
                return Ok(new { message = "Company updated successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the company" });
            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var company = _context.Companies.FirstOrDefault(c => c.Id == id && c.UserId == userId);
                if (company == null) return NotFound();

                // Check if the company has associated purchase orders
                if (_context.PurchaseOrders.Any(po => po.CompanyId == id))
                {
                    return BadRequest("Cannot delete company with associated purchase orders.");
                }

                _context.Companies.Remove(company);
                _context.SaveChanges();
                return Ok(new { message = "Company deleted successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the company" });
            }
        }
    }
}