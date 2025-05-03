using Microsoft.AspNetCore.Mvc;
using CompanyManagementSystem.Models;
using CompanyManagementSystem.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace CompanyManagementSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Company>> Get()
        {
            return _context.Companies.ToList();
        }

        [HttpGet("{id}")]
        public ActionResult<Company> Get(int id) 
        { 
            var company = _context.Companies.Find(id); 
            if (company == null) return NotFound(); 
            return company; 
        }

        [HttpPost]
        public ActionResult Post([FromBody] Company company) 
        { 
            _context.Companies.Add(company); 
            _context.SaveChanges(); 
            return Ok(); 
        }

        [HttpPut("{id}")]
        public ActionResult Put(int id, [FromBody] Company company) 
        { 
            var existing = _context.Companies.Find(id); 
            if (existing == null) return NotFound(); 
            existing.Name = company.Name; 
            existing.Address = company.Address; 
            _context.SaveChanges(); 
            return Ok(); 
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var company = _context.Companies.Find(id);
            if (company == null) return NotFound();

            // Check if the company has associated purchase orders
            if (_context.PurchaseOrders.Any(po => po.CompanyId == id))
            {
                return BadRequest("Cannot delete company with associated purchase orders.");
            }

            _context.Companies.Remove(company);
            _context.SaveChanges();
            return Ok();
        }
    }
}