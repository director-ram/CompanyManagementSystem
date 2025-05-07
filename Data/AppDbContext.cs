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

            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Address).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasOne(po => po.Company)
                    .WithMany()
                    .HasForeignKey(po => po.CompanyId)
                    .IsRequired(false);

                entity.HasOne(po => po.User)
                    .WithMany(u => u.PurchaseOrders)
                    .HasForeignKey(po => po.UserId);
            });

            modelBuilder.Entity<LineItem>(entity =>
            {
                entity.HasOne(li => li.PurchaseOrder)
                    .WithMany(po => po.LineItems)
                    .HasForeignKey(li => li.PurchaseOrderId);
            });
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