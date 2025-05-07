using System;
using System.Collections.Generic;

namespace CompanyManagementSystem.Models
{
    public class User
    {
        public User()
        {
            Username = string.Empty;
            PasswordHash = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            PurchaseOrders = new List<PurchaseOrder>();
        }

        public int Id { get; set; } // Assuming Id is your primary key
        public string Username { get; set; }
        // Add the PasswordHash property
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        // Add other user properties as needed (e.g., Email, Role)

        // Navigation property for Purchase Orders (if you added UserId to PurchaseOrder)
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; }
    }
}
