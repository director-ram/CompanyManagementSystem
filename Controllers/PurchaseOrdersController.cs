using Microsoft.AspNetCore.Mvc;
using CompanyManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CompanyManagementSystem.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CompanyManagementSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PurchaseOrdersController> _logger;

        public PurchaseOrdersController(AppDbContext context, ILogger<PurchaseOrdersController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current authenticated user's ID from claims
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return int.Parse(userIdClaim.Value);
        }

        /// <summary>
        /// Gets all purchase orders for the current user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PurchaseOrder>>> Get()
        {
            try
            {
                var userId = GetCurrentUserId();
                var purchaseOrders = await _context.PurchaseOrders
                    .Include(po => po.LineItems)
                    .Include(po => po.Company)
                    .Where(po => po.UserId == userId)
                    .ToListAsync();

                return purchaseOrders;
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase orders");
                return StatusCode(500, new { message = "An error occurred while retrieving purchase orders" });
            }
        }

        /// <summary>
        /// Gets a specific purchase order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrder>> Get(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.LineItems)
                    .FirstOrDefaultAsync(po => po.Id == id && po.UserId == userId);
                
                if (purchaseOrder == null) return NotFound();

                if (purchaseOrder.CompanyId.HasValue)
                {
                    var company = await _context.Companies
                        .Where(c => c.Id == purchaseOrder.CompanyId.Value)
                        .Select(c => new Company
                        {
                            Id = c.Id,
                            Name = c.Name,
                            Address = c.Address,
                            UserId = c.UserId
                        })
                        .FirstOrDefaultAsync();
                        
                    purchaseOrder.Company = company;
                }
                
                return purchaseOrder;
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving purchase order {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the purchase order" });
            }
        }

        /// <summary>
        /// Creates a new purchase order
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] PurchaseOrderDto purchaseOrderDto)
        {
            if (purchaseOrderDto == null)
            {
                return BadRequest("Purchase order is required.");
            }

            if (!purchaseOrderDto.CompanyId.HasValue)
            {
                return BadRequest("CompanyId is required.");
            }

            if (purchaseOrderDto.LineItems == null || !purchaseOrderDto.LineItems.Any())
            {
                return BadRequest("At least one line item is required.");
            }

            // Validate line items
            foreach (var lineItem in purchaseOrderDto.LineItems)
            {
                if (lineItem.Quantity <= 0)
                {
                    return BadRequest($"Invalid quantity for product {lineItem.ProductId}. Quantity must be greater than 0.");
                }
                if (lineItem.UnitPrice < 0)
                {
                    return BadRequest($"Invalid unit price for product {lineItem.ProductId}. Price cannot be negative.");
                }
            }

            // Validate order date
            if (purchaseOrderDto.OrderDate.Date < DateTime.Today)
            {
                return BadRequest("Order date cannot be in the past.");
            }

            try
            {
                var userId = GetCurrentUserId();
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Id == purchaseOrderDto.CompanyId && c.UserId == userId);

                if (company == null)
                {
                    return BadRequest($"Company not found or you don't have access to it. CompanyId: {purchaseOrderDto.CompanyId.Value}");
                }

                var purchaseOrder = new PurchaseOrder
                {
                    CompanyId = purchaseOrderDto.CompanyId.Value,
                    Company = null,
                    OrderDate = purchaseOrderDto.OrderDate,
                    TotalAmount = purchaseOrderDto.TotalAmount,
                    NotificationEmail = purchaseOrderDto.NotificationEmail,
                    UserId = userId,
                    LineItems = new List<LineItem>()
                };

                if (!string.IsNullOrEmpty(purchaseOrder.NotificationEmail))
                {
                    purchaseOrder.NotificationTime = purchaseOrder.OrderDate.Date == DateTime.Today
                        ? DateTime.Now.AddSeconds(10)
                        : DateTime.Now.AddMinutes(5);
                }

                foreach (var lineItemDto in purchaseOrderDto.LineItems)
                {
                    var lineItem = new LineItem
                    {
                        ProductId = lineItemDto.ProductId,
                        Quantity = lineItemDto.Quantity,
                        UnitPrice = lineItemDto.UnitPrice
                    };
                    purchaseOrder.LineItems.Add(lineItem);
                }

                // Validate total amount matches line items
                var calculatedTotal = purchaseOrder.LineItems.Sum(li => li.Quantity * li.UnitPrice);
                if (Math.Abs(calculatedTotal - purchaseOrder.TotalAmount) > 0.01m)
                {
                    return BadRequest("Total amount does not match the sum of line items.");
                }

                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();
                return Ok(new { id = purchaseOrder.Id, message = "Purchase order created successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase order");
                return StatusCode(500, new { message = "An error occurred while creating the purchase order" });
            }
        }

        /// <summary>
        /// Deletes a purchase order by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.LineItems)
                    .FirstOrDefaultAsync(po => po.Id == id && po.UserId == userId);
                
                if (purchaseOrder == null)
                {
                    return NotFound($"Purchase order with ID {id} not found or you don't have access to it.");
                }

                if (purchaseOrder.LineItems != null)
                {
                    _context.LineItems.RemoveRange(purchaseOrder.LineItems);
                }

                _context.PurchaseOrders.Remove(purchaseOrder);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Purchase order {id} deleted successfully" });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting purchase order {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the purchase order" });
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