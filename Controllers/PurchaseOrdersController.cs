using Microsoft.AspNetCore.Mvc;
using CompanyManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CompanyManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CompanyManagementSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PurchaseOrdersController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return int.Parse(userIdClaim.Value);
        }

        [HttpGet]
        public ActionResult<List<PurchaseOrder>> Get()
        {
            try
            {
                var userId = GetCurrentUserId();
                var purchaseOrders = _context.PurchaseOrders
                    .Include(po => po.LineItems)
                    .Where(po => po.UserId == userId)
                    .ToList();
                
                foreach (var order in purchaseOrders)
                {
                    if (order.CompanyId.HasValue)
                    {
                        try
                        {
                            order.Company = _context.Companies.Find(order.CompanyId!.Value) ?? new Company 
                            { 
                                Id = order.CompanyId.Value,
                                Name = "Unknown Company", 
                                Address = "Unknown Address" 
                            };
                        }
                        catch
                        {
                            order.Company = new Company 
                            { 
                                Id = order.CompanyId.Value,
                                Name = "Unknown Company", 
                                Address = "Unknown Address" 
                            };
                        }
                    }
                }
                
                return purchaseOrders;
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving purchase orders: {ex.Message}");
                return new List<PurchaseOrder>();
            }
        }

        [HttpGet("{id}")]
        public ActionResult<PurchaseOrder> Get(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var purchaseOrder = _context.PurchaseOrders
                    .Include(po => po.LineItems)
                    .FirstOrDefault(po => po.Id == id && po.UserId == userId);
                
                if (purchaseOrder == null) return NotFound();
                
                if (purchaseOrder.CompanyId.HasValue)
                {
                    try
                    {
                        purchaseOrder.Company = _context.Companies.Find(purchaseOrder.CompanyId!.Value) ?? new Company 
                        { 
                            Id = purchaseOrder.CompanyId.Value,
                            Name = "Unknown Company", 
                            Address = "Unknown Address" 
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error loading company for purchase order {id}: {ex.Message}");
                        purchaseOrder.Company = new Company 
                        { 
                            Id = purchaseOrder.CompanyId.Value,
                            Name = "Unknown Company", 
                            Address = "Unknown Address" 
                        };
                    }
                }
                
                return purchaseOrder;
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving purchase order {id}: {ex.Message}");
                return StatusCode(500, new { message = $"Error retrieving purchase order: {ex.Message}" });
            }
        }

        [HttpPost]
        public ActionResult Post([FromBody] PurchaseOrderDto purchaseOrderDto)
        {
            if (purchaseOrderDto == null)
            {
                return BadRequest("Purchase order is required.");
            }

            if (!purchaseOrderDto.CompanyId.HasValue)
            {
                return BadRequest("CompanyId is required. CompanyId: " + purchaseOrderDto.CompanyId);
            }

            try
            {
                var userId = GetCurrentUserId();
                var company = _context.Companies.Find(purchaseOrderDto.CompanyId);
                if (company == null)
                {
                    return BadRequest("Company not found. CompanyId: " + purchaseOrderDto.CompanyId.Value);
                }

                var purchaseOrder = new PurchaseOrder
                {
                    CompanyId = purchaseOrderDto.CompanyId.Value,
                    Company = company,
                    OrderDate = purchaseOrderDto.OrderDate,
                    TotalAmount = purchaseOrderDto.TotalAmount,
                    NotificationEmail = purchaseOrderDto.NotificationEmail,
                    UserId = userId,
                    LineItems = new List<LineItem>()
                };

                // Set notification time if email is provided
                if (!string.IsNullOrEmpty(purchaseOrder.NotificationEmail))
                {
                    // If order date is today, set notification for 10 seconds from now
                    if (purchaseOrder.OrderDate.Date == DateTime.Today)
                    {
                        purchaseOrder.NotificationTime = DateTime.Now.AddSeconds(10);
                    }
                    else
                    {
                        // For future dated orders, keep the default 5 minute delay
                        purchaseOrder.NotificationTime = DateTime.Now.AddMinutes(5);
                    }
                }

                if (purchaseOrderDto.LineItems != null)
                {
                    foreach (var lineItemDto in purchaseOrderDto.LineItems)
                    {
                        var lineItem = new LineItem
                        {
                            ProductId = lineItemDto.ProductId,
                            Quantity = lineItemDto.Quantity,
                            UnitPrice = lineItemDto.UnitPrice,
                            PurchaseOrder = purchaseOrder
                        };
                        purchaseOrder.LineItems.Add(lineItem);
                        _context.LineItems.Add(lineItem);
                    }
                }

                _context.PurchaseOrders.Add(purchaseOrder);
                _context.SaveChanges();
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Purchase order creation failed: " + ex.Message + ". purchaseOrderDto: " + System.Text.Json.JsonSerializer.Serialize(purchaseOrderDto) });
            }
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var purchaseOrder = _context.PurchaseOrders.FirstOrDefault(po => po.Id == id && po.UserId == userId);
                if (purchaseOrder == null)
                {
                    return NotFound();
                }

                _context.PurchaseOrders.Remove(purchaseOrder);
                _context.SaveChanges();
                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting purchase order {id}: {ex.Message}");
                return StatusCode(500, new { message = $"Error deleting purchase order: {ex.Message}" });
            }
        }

        public class PurchaseOrderDto
        {
            public int? CompanyId { get; set; }
            public DateTime OrderDate { get; set; }
            public decimal TotalAmount { get; set; }
            public string? NotificationEmail { get; set; }
            public required List<LineItemDto> LineItems { get; set; }
        }

        public class LineItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}