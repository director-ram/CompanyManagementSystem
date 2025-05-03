using System;
using System.Collections.Generic;

namespace CompanyManagementSystem.Models
{
    public class User
    {
        public int Id { get; set; } // Assuming Id is your primary key
        public required string Username { get; set; }
        // Add the PasswordHash property
        public required string PasswordHash { get; set; }
        // Add other user properties as needed (e.g., Email, Role)

        // Navigation property for Purchase Orders (if you added UserId to PurchaseOrder)
        public required ICollection<PurchaseOrder> PurchaseOrders { get; set; }
    }
}
