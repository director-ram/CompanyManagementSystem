using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CompanyManagementSystem.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? NotificationEmail { get; set; }
        public DateTime? NotificationTime { get; set; }
        public List<LineItem>? LineItems { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
