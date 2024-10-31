using Microsoft.EntityFrameworkCore;
using TowFast_API.Models;

namespace TowFast_API.Context
{
    public class TowFastDbContext : DbContext
    {
        public TowFastDbContext(DbContextOptions<TowFastDbContext> options)
            : base(options)
        {
        }

        public DbSet<LogarModel> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
