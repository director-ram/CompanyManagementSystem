using Microsoft.EntityFrameworkCore;
using CompanyManagementSystem.Models;

namespace CompanyManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<LineItem> LineItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Company)
                .WithMany()
                .HasForeignKey(po => po.CompanyId);

            modelBuilder.Entity<LineItem>()
                .HasOne(li => li.PurchaseOrder)
                .WithMany(po => po.LineItems)
                .HasForeignKey(li => li.PurchaseOrderId);
        }
    }
}
