using Microsoft.EntityFrameworkCore;
using CompanyManagementSystem.Models;

namespace CompanyManagementSystem.Data
{
    public class UsersDbContext : DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}