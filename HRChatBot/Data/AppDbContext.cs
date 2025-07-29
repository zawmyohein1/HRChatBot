using HRChatBot.Models;
using Microsoft.EntityFrameworkCore;
using HRChatBot.Models;

namespace HRChatBot.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Leave> Leaves { get; set; }
    }
}
