using System.Text.Json.Serialization;

namespace CompanyManagementSystem.Models
{
    public class LineItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        [JsonIgnore]
        public PurchaseOrder? PurchaseOrder { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}