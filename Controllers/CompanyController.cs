using Microsoft.AspNetCore.Mvc;
using CompanyManagementSystem.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CompanyManagementSystem.Data;

[Route("api/[controller]")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public CompanyController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public ActionResult<List<Company>> Get()
    {
        return _context.Companies.ToList();
    }

    [HttpGet("{id}")]
    public ActionResult<Company> Get(int id)
    {
        var company = _context.Companies.Find(id);
        if (company == null)
        {
            return NotFound();
        }
        return company;
    }

    [HttpPost]
    public ActionResult Post([FromBody] Company company)
    {
        try
        {
            _context.Companies.Add(company);
            _context.SaveChanges();
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Company creation failed: " + ex.Message });
        }
    }

    [HttpPut("{id}")]
    public ActionResult Put(int id, [FromBody] Company company)
    {
        var existing = _context.Companies.Find(id);
        if (existing == null)
        {
            return NotFound();
        }
        existing.Name = company.Name;
        existing.Address = company.Address;
        _context.SaveChanges();
        return Ok();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var company = _context.Companies.Find(id);
        if (company == null)
        {
            return NotFound();
        }
        _context.Companies.Remove(company);
        _context.SaveChanges();
        return Ok();
    }
}
