using Microsoft.EntityFrameworkCore;
using CompanyManagementSystem.Models;
using Microsoft.Extensions.Configuration;

namespace CompanyManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<LineItem> LineItems { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LineItem>()
                .Property(li => li.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseOrder>()
                .Property(po => po.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Company)
                .WithMany()
                .HasForeignKey(po => po.CompanyId);

            modelBuilder.Entity<LineItem>()
                .HasOne(li => li.PurchaseOrder)
                .WithMany(po => po.LineItems)
                .HasForeignKey(li => li.PurchaseOrderId);
                
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.User)
                .WithMany(u => u.PurchaseOrders)
                .HasForeignKey(po => po.UserId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CompanyManagementSystem;Trusted_Connection=True;MultipleActiveResultSets=true");
            }
        }
    }
}