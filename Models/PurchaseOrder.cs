using System;
using System.Collections.Generic;

namespace CompanyManagementSystem.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public int? CompanyId { get; set; }
        public required Company Company { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? NotificationEmail { get; set; }
        public DateTime? NotificationTime { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public List<LineItem>? LineItems { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public int? UserId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public User? User { get; set; }
    }
}
